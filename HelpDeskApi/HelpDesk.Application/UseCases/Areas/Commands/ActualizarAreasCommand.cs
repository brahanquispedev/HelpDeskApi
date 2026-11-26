using System.ComponentModel.DataAnnotations;
using HelpDesk.Application.Interfaces;
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

internal sealed class ActualizarAreasCommandHandler(IUnitOfWork _unitOfWork, IAccessRules access)
    : IRequestHandler<ActualizarAreasCommand, ResponseDto<AreasResponseDto>>
{
    public async Task<ResponseDto<AreasResponseDto>> Handle(ActualizarAreasCommand request, CancellationToken ct)
    {
        try
        {
            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            if (request.IdArea == Guid.Empty)
            {
                throw new ValidationException("IdArea debe ser un identificador valido.");
            }
            if (request.IdSede == Guid.Empty)
            {
                throw new ValidationException("IdSede debe ser un identificador valido.");
            }
            if (!access.Admin) access.Permission("organizacion.administrar");

            var repo = _unitOfWork.Repository<Domain.Models.Areas>();
            var entity = await repo.GetByIdAsync([request.IdArea]);
            if (entity == null)
            {
                throw new BusinessException(404, "Area no encontrada.");
            }

            var sede = await _unitOfWork.Repository<Domain.Models.Sedes>().GetByIdAsync([request.IdSede]);
            if (!(sede != null))
            {
                throw new BusinessException(400, "La sede especificada no existe.");
            }

            var existeNombre = await repo.Query().AnyAsync(
                a => a.IdArea != request.IdArea && a.IdSede == request.IdSede && a.Nombre.ToLower() == request.Nombre.Trim().ToLower(),
                ct
            );
            if (!(!existeNombre))
            {
                throw new BusinessException(400, "Ya existe otra area con el mismo nombre en esta sede.");
            }

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
        catch (ValidationException ex)
        {
            return new ResponseDto<AreasResponseDto>(400, default, ex.Message);
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