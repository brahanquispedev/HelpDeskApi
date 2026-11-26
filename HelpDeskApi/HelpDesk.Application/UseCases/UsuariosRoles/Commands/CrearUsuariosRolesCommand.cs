using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Application.Interfaces;
using HelpDesk.Domain.Dtos.UsuariosRoles;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
namespace HelpDesk.Application.UseCases.UsuariosRoles.Commands;

public class CrearUsuariosRolesCommand : IRequest<ResponseDto<UsuariosRolesResponseDto>>
{
    public Guid IdUsuario { get; set; }
    public Guid IdRol { get; set; }
}

internal sealed class CrearUsuariosRolesCommandHandler(IUnitOfWork _unitOfWork, IAccessRules access) : IRequestHandler<CrearUsuariosRolesCommand, ResponseDto<UsuariosRolesResponseDto>>
{
    public async Task<ResponseDto<UsuariosRolesResponseDto>> Handle(CrearUsuariosRolesCommand request, CancellationToken ct)
    {
        try
        {

            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            if (request.IdUsuario == Guid.Empty)
            {
                throw new ValidationException("IdUsuario debe ser un identificador valido.");
            }
            if (request.IdRol == Guid.Empty)
            {
                throw new ValidationException("IdRol debe ser un identificador valido.");
            }
            if (!(access.Admin))
            {
                throw new BusinessException(403, "Esta operacion requiere administracion.");
            }
            var entity = new global::HelpDesk.Domain.Models.UsuariosRoles()
            {
                IdUsuario = request.IdUsuario,
                IdRol = request.IdRol,
                FechaAsignacion = DateTime.UtcNow
            };
            await Required<global::HelpDesk.Domain.Models.Usuarios>(entity.IdUsuario);
            await Required<global::HelpDesk.Domain.Models.Roles>(entity.IdRol);
            await _unitOfWork.Repository<global::HelpDesk.Domain.Models.UsuariosRoles>().AddAsync(entity);
            await _unitOfWork.Complete();
            return new ResponseDto<UsuariosRolesResponseDto>(201, new UsuariosRolesResponseDto
            {
                IdUsuario = entity.IdUsuario,
                IdRol = entity.IdRol,
                FechaAsignacion = entity.FechaAsignacion
            }, "Registro creado.");
        }
        catch (ValidationException ex)
        {
            return new ResponseDto<UsuariosRolesResponseDto>(400, default, ex.Message);
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<UsuariosRolesResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<UsuariosRolesResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }

    private async Task<T> Required<T>(Guid id) where T : class =>
        await _unitOfWork.Repository<T>().GetByIdAsync([id]) ?? throw new BusinessException(404, "Registro relacionado no encontrado.");

}