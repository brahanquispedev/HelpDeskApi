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

public class CrearSedesCommand : IRequest<ResponseDto<SedesResponseDto>>
{
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = "";

    [StringLength(250)]
    public string? Direccion { get; set; }
}

internal sealed class CrearSedesCommandHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<CrearSedesCommand, ResponseDto<SedesResponseDto>>
{
    public async Task<ResponseDto<SedesResponseDto>> Handle(CrearSedesCommand request, CancellationToken ct)
    {
        try
        {
            Validation.Check(request);
            if (!access.Admin) access.Permission("sedes.crear");

            var repo = _unitOfWork.Repository<Domain.Models.Sedes>();

            var existeNombre = await repo.Query().AnyAsync(s => s.Nombre.ToLower() == request.Nombre.Trim().ToLower(), ct);
            Validation.Require(!existeNombre, "Ya existe una sede con el mismo nombre.");

            var entity = new Domain.Models.Sedes
            {
                IdSede = Guid.NewGuid(),
                Nombre = request.Nombre.Trim(),
                Direccion = request.Direccion?.Trim(),
                Activo = true
            };

            await repo.AddAsync(entity);
            await _unitOfWork.Complete();

            var dto = new SedesResponseDto
            {
                IdSede = entity.IdSede,
                Nombre = entity.Nombre,
                Direccion = entity.Direccion,
                Activo = entity.Activo
            };

            return new ResponseDto<SedesResponseDto>(201, dto, "Sede creada correctamente.");
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