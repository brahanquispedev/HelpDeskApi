using System.ComponentModel.DataAnnotations;
using HelpDesk.Application.Common;
using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Areas;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.UseCases.Areas.Commands;

public class ActualizarAreasCommand : IRequest<ResponseDto<AreasResponseDto>>
{
    public Guid IdArea { get; set; }
    public Guid IdSede { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = "";

    public bool Activo { get; set; }
}

internal sealed class ActualizarAreasCommandHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<ActualizarAreasCommand, ResponseDto<AreasResponseDto>>
{
    public async Task<ResponseDto<AreasResponseDto>> Handle(ActualizarAreasCommand request, CancellationToken ct)
    {
        try
        {
            Validation.Check(request);
            if (!access.Admin) access.Permission("areas.actualizar");

            var repo = _unitOfWork.Repository<Domain.Models.Areas>();
            var entity = await repo.GetByIdAsync([request.IdArea]);
            if (entity == null)
            {
                throw new BusinessException(404, "Area no encontrada.");
            }

            var sede = await _unitOfWork.Repository<Domain.Models.Sedes>().GetByIdAsync([request.IdSede]);
            Validation.Require(sede != null, "La sede especificada no existe.");

            var existeNombre = await repo.Query().AnyAsync(
                a => a.IdArea != request.IdArea && a.IdSede == request.IdSede && a.Nombre.ToLower() == request.Nombre.Trim().ToLower(),
                ct
            );
            Validation.Require(!existeNombre, "Ya existe otra area con el mismo nombre en esta sede.");

            entity.IdSede = request.IdSede;
            entity.Nombre = request.Nombre.Trim();
            entity.Activo = request.Activo;

            repo.Update(entity);
            await _unitOfWork.Complete();

            var dto = new AreasResponseDto
            {
                IdArea = entity.IdArea,
                IdSede = entity.IdSede,
                Nombre = entity.Nombre,
                Activo = entity.Activo
            };

            return new ResponseDto<AreasResponseDto>(200, dto, "Area actualizada correctamente.");
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<AreasResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<AreasResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }
}