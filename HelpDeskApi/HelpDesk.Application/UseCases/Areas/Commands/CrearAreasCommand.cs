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
            Validation.Check(request);
            if (!access.Admin) access.Permission("areas.crear");

            // Validar que la sede padre exista
            var sede = await _unitOfWork.Repository<Domain.Models.Sedes>().GetByIdAsync([request.IdSede]);
            Validation.Require(sede != null, "La sede especificada no existe.");
            Validation.Require(sede!.Activo, "No se pueden crear areas en una sede inactiva.");

            var repo = _unitOfWork.Repository<Domain.Models.Areas>();

            // Validar unicidad del nombre dentro de la misma sede
            var existeNombre = await repo.Query().AnyAsync(
                a => a.IdSede == request.IdSede && a.Nombre.ToLower() == request.Nombre.Trim().ToLower(), 
                ct
            );
            Validation.Require(!existeNombre, "Ya existe un area con el mismo nombre en esta sede.");

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