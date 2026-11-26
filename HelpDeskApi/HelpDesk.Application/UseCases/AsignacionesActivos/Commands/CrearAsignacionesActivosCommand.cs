using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Application.Interfaces;
using HelpDesk.Domain.Dtos.AsignacionesActivos;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
namespace HelpDesk.Application.UseCases.AsignacionesActivos.Commands;

public class CrearAsignacionesActivosCommand : IRequest<ResponseDto<AsignacionesActivosResponseDto>>
{
    public Guid IdActivo { get; set; }
    public Guid IdUsuarioResponsable { get; set; }
    public string? Observaciones { get; set; }
}

internal sealed class CrearAsignacionesActivosCommandHandler(IUnitOfWork _unitOfWork, IAccessRules access, ICurrentUser actor) : IRequestHandler<CrearAsignacionesActivosCommand, ResponseDto<AsignacionesActivosResponseDto>>
{
    public async Task<ResponseDto<AsignacionesActivosResponseDto>> Handle(CrearAsignacionesActivosCommand request, CancellationToken ct)
    {
        try
        {

            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            if (request.IdActivo == Guid.Empty)
            {
                throw new ValidationException("IdActivo debe ser un identificador valido.");
            }
            if (request.IdUsuarioResponsable == Guid.Empty)
            {
                throw new ValidationException("IdUsuarioResponsable debe ser un identificador valido.");
            }
            if (!(access.Admin))
            {
                throw new BusinessException(403, "Esta operacion requiere administracion.");
            }
            var entity = new global::HelpDesk.Domain.Models.AsignacionesActivos()
            {
                IdAsignacion = Guid.NewGuid(),
                IdActivo = request.IdActivo,
                IdUsuarioResponsable = request.IdUsuarioResponsable,
                Observaciones = request.Observaciones,
                FechaAsignacion = DateTime.UtcNow
            };
            var asset = await Required<global::HelpDesk.Domain.Models.Activos>(entity.IdActivo);
            if (!(asset.Estado == "disponible"))
            {
                throw new BusinessException(409, "El activo no esta disponible.");
            }
            if (!((await Required<global::HelpDesk.Domain.Models.Usuarios>(entity.IdUsuarioResponsable)).Activo))
            {
                throw new BusinessException(400, "El responsable esta inactivo.");
            }
            entity.IdUsuarioAsignador = actor.Id;
            asset.Estado = "asignado";
            await _unitOfWork.Repository<global::HelpDesk.Domain.Models.AsignacionesActivos>().AddAsync(entity);
            await _unitOfWork.Complete();
            return new ResponseDto<AsignacionesActivosResponseDto>(201, new AsignacionesActivosResponseDto
            {
                IdAsignacion = entity.IdAsignacion,
                IdActivo = entity.IdActivo,
                IdUsuarioResponsable = entity.IdUsuarioResponsable,
                IdUsuarioAsignador = entity.IdUsuarioAsignador,
                FechaAsignacion = entity.FechaAsignacion,
                FechaDevolucion = entity.FechaDevolucion,
                Observaciones = entity.Observaciones
            }, "Registro creado.");
        }
        catch (ValidationException ex)
        {
            return new ResponseDto<AsignacionesActivosResponseDto>(400, default, ex.Message);
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<AsignacionesActivosResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<AsignacionesActivosResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }

    private async Task<T> Required<T>(Guid id) where T : class =>
        await _unitOfWork.Repository<T>().GetByIdAsync([id]) ?? throw new BusinessException(404, "Registro relacionado no encontrado.");

}