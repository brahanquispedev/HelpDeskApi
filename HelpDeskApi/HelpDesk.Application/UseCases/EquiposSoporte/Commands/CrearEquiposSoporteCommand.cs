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

public class CrearEquiposSoporteCommand : IRequest<ResponseDto<EquiposSoporteResponseDto>>
{
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = "";

    [StringLength(500)]
    public string? Descripcion { get; set; }
}

internal sealed class CrearEquiposSoporteCommandHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<CrearEquiposSoporteCommand, ResponseDto<EquiposSoporteResponseDto>>
{
    public async Task<ResponseDto<EquiposSoporteResponseDto>> Handle(CrearEquiposSoporteCommand request, CancellationToken ct)
    {
        try
        {
            Validation.Check(request);
            if (!access.Admin) access.Permission("equipos_soporte.crear");

            var repo = _unitOfWork.Repository<Domain.Models.EquiposSoporte>();

            var existeNombre = await repo.Query().AnyAsync(
                e => e.Nombre.ToLower() == request.Nombre.Trim().ToLower(), 
                ct
            );
            Validation.Require(!existeNombre, "Ya existe un equipo de soporte con el mismo nombre.");

            var entity = new Domain.Models.EquiposSoporte
            {
                IdEquipo = Guid.NewGuid(),
                Nombre = request.Nombre.Trim(),
                Descripcion = request.Descripcion?.Trim(),
                Activo = true
            };

            await repo.AddAsync(entity);
            await _unitOfWork.Complete();

            var dto = new EquiposSoporteResponseDto
            {
                IdEquipo = entity.IdEquipo,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion,
                Activo = entity.Activo
            };

            return new ResponseDto<EquiposSoporteResponseDto>(201, dto, "Equipo de soporte creado correctamente.");
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