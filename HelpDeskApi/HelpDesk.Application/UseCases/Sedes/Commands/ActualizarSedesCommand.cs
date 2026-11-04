using System.ComponentModel.DataAnnotations;
using HelpDesk.Application.Common;
using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.Sedes;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.UseCases.Sedes.Commands;

public class ActualizarSedesCommand : IRequest<ResponseDto<SedesResponseDto>>
{
    public Guid IdSede { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = "";

    [StringLength(250)]
    public string? Direccion { get; set; }

    public bool Activo { get; set; }
}

internal sealed class ActualizarSedesCommandHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<ActualizarSedesCommand, ResponseDto<SedesResponseDto>>
{
    public async Task<ResponseDto<SedesResponseDto>> Handle(ActualizarSedesCommand request, CancellationToken ct)
    {
        try
        {
            Validation.Check(request);
            if (!access.Admin) access.Permission("sedes.actualizar");

            var repo = _unitOfWork.Repository<Domain.Models.Sedes>();
            var entity = await repo.GetByIdAsync([request.IdSede]);
            if (entity == null)
            {
                throw new BusinessException(404, "Sede no encontrada.");
            }

            var existeNombre = await repo.Query().AnyAsync(s => s.IdSede != request.IdSede && s.Nombre.ToLower() == request.Nombre.Trim().ToLower(), ct);
            Validation.Require(!existeNombre, "Ya existe otra sede con el mismo nombre.");

            entity.Nombre = request.Nombre.Trim();
            entity.Direccion = request.Direccion?.Trim();
            entity.Activo = request.Activo;

            repo.Update(entity);
            await _unitOfWork.Complete();

            var dto = new SedesResponseDto
            {
                IdSede = entity.IdSede,
                Nombre = entity.Nombre,
                Direccion = entity.Direccion,
                Activo = entity.Activo
            };

            return new ResponseDto<SedesResponseDto>(200, dto, "Sede actualizada correctamente.");
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<SedesResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<SedesResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }
}