using System.ComponentModel.DataAnnotations;
using HelpDesk.Application.Services.Security;
using HelpDesk.Domain.Dtos.Areas;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.UseCases.Areas.Commands;

public class CrearAreasCommand : IRequest<ResponseDto<AreasResponseDto>>
{
    public Guid IdSede { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = "";
}

internal sealed class CrearAreasCommandHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<CrearAreasCommand, ResponseDto<AreasResponseDto>>
{
    public async Task<ResponseDto<AreasResponseDto>> Handle(CrearAreasCommand request, CancellationToken ct)
    {
        try
        {
            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            if (request.IdSede == Guid.Empty)
            {
                throw new ValidationException("IdSede debe ser un identificador valido.");
            }
            if (!access.Admin) access.Permission("organizacion.administrar");

            // Validar que la sede padre exista
            var sede = await _unitOfWork.Repository<Domain.Models.Sedes>().GetByIdAsync([request.IdSede]);
            if (!(sede != null))
            {
                throw new BusinessException(400, "La sede especificada no existe.");
            }
            if (!(sede!.Activo))
            {
                throw new BusinessException(400, "No se pueden crear areas en una sede inactiva.");
            }

            var repo = _unitOfWork.Repository<Domain.Models.Areas>();

            // Validar unicidad del nombre dentro de la misma sede
            var existeNombre = await repo.Query().AnyAsync(
                a => a.IdSede == request.IdSede && a.Nombre.ToLower() == request.Nombre.Trim().ToLower(), 
                ct
            );
            if (!(!existeNombre))
            {
                throw new BusinessException(400, "Ya existe un area con el mismo nombre en esta sede.");
            }

            var entity = new Domain.Models.Areas
            {
                IdArea = Guid.NewGuid(),
                IdSede = request.IdSede,
                Nombre = request.Nombre.Trim(),
                Activo = true
            };

            await repo.AddAsync(entity);
            await _unitOfWork.Complete();

            var dto = new AreasResponseDto
            {
                IdArea = entity.IdArea,
                IdSede = entity.IdSede,
                Nombre = entity.Nombre,
                Activo = entity.Activo
            };

            return new ResponseDto<AreasResponseDto>(201, dto, "Area creada correctamente.");
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