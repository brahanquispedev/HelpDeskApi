using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HelpDesk.Application.Common;
using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Activos;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ActivoEntity = HelpDesk.Domain.Models.Activos;

namespace HelpDesk.Application.UseCases.Activos.Commands;

public class CambiarEstadoActivoCommand : IRequest<ResponseDto<ActivosResponseDto>>
{
    [JsonIgnore]
    public Guid IdActivo { get; set; }

    [Required, StringLength(30)]
    public string Estado { get; set; } = string.Empty;
}

internal sealed class CambiarEstadoActivoCommandHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<CambiarEstadoActivoCommand, ResponseDto<ActivosResponseDto>>
{
    public async Task<ResponseDto<ActivosResponseDto>> Handle(CambiarEstadoActivoCommand request, CancellationToken ct)
    {
        try
        {
            Validation.Check(request);
            if (!access.Admin) access.Permission("activos.administrar");

            var entity = await _unitOfWork.Repository<ActivoEntity>().GetByIdAsync([request.IdActivo])
                ?? throw new BusinessException(404, "Activo no encontrado.");

            var estado = request.Estado.Trim().ToLowerInvariant();
            Validation.Require(estado is "disponible" or "mantenimiento" or "baja", "Estado no permitido.");
            Validation.Require(entity.Estado != "baja", "El activo esta dado de baja.", 409);
            Validation.Require(entity.Estado != estado, "El activo ya se encuentra en ese estado.", 409);

            if (estado == "baja")
            {
                var tieneAsignacionVigente = await _unitOfWork.Repository<AsignacionesActivos>().Query()
                    .AnyAsync(x => x.IdActivo == entity.IdActivo && x.FechaDevolucion == null, ct);
                Validation.Require(!tieneAsignacionVigente,
                    "No se puede dar de baja un activo con asignacion vigente.", 409);
            }
            else if (estado == "mantenimiento")
            {
                Validation.Require(entity.Estado == "disponible",
                    "Solo un activo disponible puede pasar a mantenimiento.", 409);
            }
            else if (estado == "disponible")
            {
                Validation.Require(entity.Estado == "mantenimiento",
                    "Solo un activo en mantenimiento puede volver a disponible.", 409);
            }

            entity.Estado = estado;
            await _unitOfWork.Complete();

            return new ResponseDto<ActivosResponseDto>(200, ToDto(entity), "Estado del activo actualizado.");
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<ActivosResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<ActivosResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }

    private static ActivosResponseDto ToDto(ActivoEntity entity) => new()
    {
        IdActivo = entity.IdActivo,
        IdSede = entity.IdSede,
        IdArea = entity.IdArea,
        CodigoPatrimonial = entity.CodigoPatrimonial,
        Nombre = entity.Nombre,
        IdTipoActivo = entity.IdTipoActivo,
        NumeroSerie = entity.NumeroSerie,
        Marca = entity.Marca,
        Modelo = entity.Modelo,
        Estado = entity.Estado,
        FechaRegistro = entity.FechaRegistro
    };
}
