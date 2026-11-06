using System.ComponentModel.DataAnnotations;
using HelpDesk.Application.Common;
using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.EquiposSoporte;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.UseCases.EquiposSoporte.Commands;

public class ActualizarEquiposSoporteCommand : IRequest<ResponseDto<EquiposSoporteResponseDto>>
{
    public Guid IdEquipo { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = "";

    [StringLength(500)]
    public string? Descripcion { get; set; }

    public bool Activo { get; set; }
}

internal sealed class ActualizarEquiposSoporteCommandHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<ActualizarEquiposSoporteCommand, ResponseDto<EquiposSoporteResponseDto>>
{
    public async Task<ResponseDto<EquiposSoporteResponseDto>> Handle(ActualizarEquiposSoporteCommand request, CancellationToken ct)
    {
        try
        {
            Validation.Check(request);
            if (!access.Admin) access.Permission("equipos_soporte.actualizar");

            var repo = _unitOfWork.Repository<Domain.Models.EquiposSoporte>();
            var entity = await repo.GetByIdAsync([request.IdEquipo]);
            if (entity == null)
            {
                throw new BusinessException(404, "Equipo de soporte no encontrado.");
            }

            var existeNombre = await repo.Query().AnyAsync(
                e => e.IdEquipo != request.IdEquipo && e.Nombre.ToLower() == request.Nombre.Trim().ToLower(),
                ct
            );
            Validation.Require(!existeNombre, "Ya existe otro equipo de soporte con el mismo nombre.");

            entity.Nombre = request.Nombre.Trim();
            entity.Descripcion = request.Descripcion?.Trim();
            entity.Activo = request.Activo;

            repo.Update(entity);
            await _unitOfWork.Complete();

            var dto = new EquiposSoporteResponseDto
            {
                IdEquipo = entity.IdEquipo,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion,
                Activo = entity.Activo
            };

            return new ResponseDto<EquiposSoporteResponseDto>(200, dto, "Equipo de soporte actualizado correctamente.");
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<EquiposSoporteResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<EquiposSoporteResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }
}