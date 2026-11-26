using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HelpDesk.Application.Interfaces;
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

internal sealed class CambiarEstadoActivoCommandHandler(IUnitOfWork _unitOfWork, IAccessRules access)
    : IRequestHandler<CambiarEstadoActivoCommand, ResponseDto<ActivosResponseDto>>
{
    public async Task<ResponseDto<ActivosResponseDto>> Handle(CambiarEstadoActivoCommand request, CancellationToken ct)
    {
        try
        {
            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            if (request.IdActivo == Guid.Empty)
            {
                throw new ValidationException("IdActivo debe ser un identificador valido.");
            }
            if (!access.Admin) access.Permission("activos.administrar");

            var entity = await _unitOfWork.Repository<ActivoEntity>().GetByIdAsync([request.IdActivo])
                ?? throw new BusinessException(404, "Activo no encontrado.");

            var estado = request.Estado.Trim().ToLowerInvariant();
            if (estado == "baja") estado = "retirado";
            if (!(estado is "disponible" or "mantenimiento" or "retirado"))
            {
                throw new BusinessException(400, "Estado no permitido.");
            }
            if (!(entity.Estado is not ("baja" or "retirado" or "perdido")))
            {
                throw new BusinessException(409, "El activo ya no esta operativo.");
            }
            if (!(entity.Estado != estado))
            {
                throw new BusinessException(409, "El activo ya se encuentra en ese estado.");
            }

            if (estado == "retirado")
            {
                var tieneAsignacionVigente = await _unitOfWork.Repository<global::HelpDesk.Domain.Models.AsignacionesActivos>().Query()
                    .AnyAsync(x => x.IdActivo == entity.IdActivo && x.FechaDevolucion == null, ct);
                if (!(!tieneAsignacionVigente))
                {
                    throw new BusinessException(409, "No se puede dar de baja un activo con asignacion vigente.");
                }
            }
            else if (estado == "mantenimiento")
            {
                if (!(entity.Estado == "disponible"))
                {
                    throw new BusinessException(409, "Solo un activo disponible puede pasar a mantenimiento.");
                }
            }
            else if (estado == "disponible")
            {
                if (!(entity.Estado == "mantenimiento"))
                {
                    throw new BusinessException(409, "Solo un activo en mantenimiento puede volver a disponible.");
                }
            }

            entity.Estado = estado;
            await _unitOfWork.Complete();

            return new ResponseDto<ActivosResponseDto>(200, ToDto(entity), "Estado del activo actualizado.");
        }
        catch (ValidationException ex)
        {
            return new ResponseDto<ActivosResponseDto>(400, default, ex.Message);
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