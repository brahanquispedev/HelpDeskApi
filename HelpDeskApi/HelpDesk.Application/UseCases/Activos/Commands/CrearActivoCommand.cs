using System.ComponentModel.DataAnnotations;
using HelpDesk.Application.Common;
using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Activos;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using ActivoEntity = HelpDesk.Domain.Models.Activos;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.UseCases.Activos.Commands;

public class CrearActivoCommand : IRequest<ResponseDto<ActivosResponseDto>>
{
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

    [StringLength(30)]
    public string Estado { get; set; } = "disponible";
}

internal sealed class CrearActivoCommandHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<CrearActivoCommand, ResponseDto<ActivosResponseDto>>
{
    public async Task<ResponseDto<ActivosResponseDto>> Handle(CrearActivoCommand request, CancellationToken ct)
    {
        try
        {
            Validation.Check(request);
            if (!access.Admin) access.Permission("activos.administrar");

            var codigo = request.CodigoPatrimonial.Trim();
            var existeCodigo = await _unitOfWork.Repository<ActivoEntity>().Query()
                .AnyAsync(x => x.CodigoPatrimonial.ToLower() == codigo.ToLower(), ct);
            Validation.Require(!existeCodigo, "Ya existe un activo con ese codigo patrimonial.", 409);

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

            var estado = request.Estado.Trim().ToLowerInvariant();
            Validation.Require(estado is "disponible" or "mantenimiento", "Estado inicial no permitido.");

            var entity = new ActivoEntity
            {
                IdActivo = Guid.NewGuid(),
                IdSede = request.IdSede,
                IdArea = request.IdArea,
                CodigoPatrimonial = codigo,
                Nombre = request.Nombre.Trim(),
                IdTipoActivo = request.IdTipoActivo,
                NumeroSerie = request.NumeroSerie?.Trim(),
                Marca = request.Marca?.Trim(),
                Modelo = request.Modelo?.Trim(),
                Estado = estado,
                FechaRegistro = DateTime.UtcNow
            };

            await _unitOfWork.Repository<ActivoEntity>().AddAsync(entity);
            await _unitOfWork.Complete();

            return new ResponseDto<ActivosResponseDto>(201, ToDto(entity), "Activo creado.");
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
