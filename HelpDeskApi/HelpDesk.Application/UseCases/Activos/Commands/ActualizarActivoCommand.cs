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

public class ActualizarActivoCommand : IRequest<ResponseDto<ActivosResponseDto>>
{
    [JsonIgnore]
    public Guid IdActivo { get; set; }

    public Guid IdSede { get; set; }
    public Guid? IdArea { get; set; }

    [Required, StringLength(80)]
    public string CodigoPatrimonial { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    public Guid IdTipoActivo { get; set; }

    [StringLength(150)]
    public string? NumeroSerie { get; set; }

    [StringLength(100)]
    public string? Marca { get; set; }

    [StringLength(100)]
    public string? Modelo { get; set; }
}

internal sealed class ActualizarActivoCommandHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<ActualizarActivoCommand, ResponseDto<ActivosResponseDto>>
{
    public async Task<ResponseDto<ActivosResponseDto>> Handle(ActualizarActivoCommand request, CancellationToken ct)
    {
        try
        {
            Validation.Check(request);
            if (!access.Admin) access.Permission("activos.administrar");

            var entity = await _unitOfWork.Repository<ActivoEntity>().GetByIdAsync([request.IdActivo])
                ?? throw new BusinessException(404, "Activo no encontrado.");

            var codigo = request.CodigoPatrimonial.Trim();
            var existeCodigo = await _unitOfWork.Repository<ActivoEntity>().Query()
                .AnyAsync(x => x.IdActivo != request.IdActivo && x.CodigoPatrimonial.ToLower() == codigo.ToLower(), ct);
            Validation.Require(!existeCodigo, "Ya existe otro activo con ese codigo patrimonial.", 409);

            var sede = await _unitOfWork.Repository<Sedes>().GetByIdAsync([request.IdSede])
                ?? throw new BusinessException(400, "Sede no encontrada.");
            Validation.Require(sede.Activo, "La sede esta inactiva.");

            var tipo = await _unitOfWork.Repository<TiposActivos>().GetByIdAsync([request.IdTipoActivo])
                ?? throw new BusinessException(400, "Tipo de activo no encontrado.");
            Validation.Require(tipo.Activo, "El tipo de activo esta inactivo.");

            if (request.IdArea.HasValue)
            {
                Validation.Require(request.IdArea.Value != Guid.Empty, "IdArea no es valido.");
                var area = await _unitOfWork.Repository<Areas>().GetByIdAsync([request.IdArea.Value])
                    ?? throw new BusinessException(400, "Area no encontrada.");
                Validation.Require(area.Activo && area.IdSede == request.IdSede,
                    "El area no pertenece a la sede o esta inactiva.");
            }

            entity.IdSede = request.IdSede;
            entity.IdArea = request.IdArea;
            entity.CodigoPatrimonial = codigo;
            entity.Nombre = request.Nombre.Trim();
            entity.IdTipoActivo = request.IdTipoActivo;
            entity.NumeroSerie = request.NumeroSerie?.Trim();
            entity.Marca = request.Marca?.Trim();
            entity.Modelo = request.Modelo?.Trim();

            await _unitOfWork.Complete();

            return new ResponseDto<ActivosResponseDto>(200, ToDto(entity), "Activo actualizado.");
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
