using HelpDesk.Application.Interfaces;
using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Dtos.Autenticacion;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace HelpDesk.Application.UseCases.Autenticacion.Commands;

public sealed class RegistrarUsuarioCommand : RegistroDto, IRequest<ResponseDto<UsuarioRegistradoDto>>
{
}

internal sealed class RegistrarUsuarioCommandHandler(IUnitOfWork _unitOfWork, IPasswordService _passwordService)
    : IRequestHandler<RegistrarUsuarioCommand, ResponseDto<UsuarioRegistradoDto>>
{
    public async Task<ResponseDto<UsuarioRegistradoDto>> Handle(RegistrarUsuarioCommand request, CancellationToken ct)
    {
        try
        {
            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            var nombreUsuario = request.NombreUsuario.Trim();
            var correo = request.Correo.Trim().ToLowerInvariant();

            var existe = await _unitOfWork.Repository<global::HelpDesk.Domain.Models.Usuarios>().Query()
                .AnyAsync(u => u.NombreUsuario.ToLower() == nombreUsuario.ToLower() || u.Correo == correo, ct);
            if (!(!existe))
            {
                throw new BusinessException(409, "El nombre de usuario o el correo ya estan registrados.");
            }

            var rolEmpleado = await _unitOfWork.Repository<global::HelpDesk.Domain.Models.Roles>().Query()
                .SingleOrDefaultAsync(r => r.Nombre == "empleado");
            if (!(rolEmpleado is not null))
            {
                throw new BusinessException(409, "Falta el rol empleado del catalogo inicial.");
            }

            var usuario = new global::HelpDesk.Domain.Models.Usuarios
            {
                IdUsuario = Guid.NewGuid(),
                Nombres = request.Nombres.Trim(),
                Apellidos = request.Apellidos.Trim(),
                NombreUsuario = nombreUsuario,
                Correo = correo,
                ClaveHash = _passwordService.Hash(request.Clave),
                Activo = true,
                FechaRegistro = DateTime.UtcNow
            };

            await _unitOfWork.Repository<global::HelpDesk.Domain.Models.Usuarios>().AddAsync(usuario);
            await _unitOfWork.Repository<global::HelpDesk.Domain.Models.UsuariosRoles>().AddAsync(new global::HelpDesk.Domain.Models.UsuariosRoles
            {
                IdUsuario = usuario.IdUsuario,
                IdRol = rolEmpleado!.IdRol,
                FechaAsignacion = DateTime.UtcNow
            });
            await _unitOfWork.Complete();

            return new ResponseDto<UsuarioRegistradoDto>(
                201,
                new UsuarioRegistradoDto(usuario.IdUsuario, usuario.NombreUsuario));
        }
        catch (ValidationException ex)
        {
            return new ResponseDto<UsuarioRegistradoDto>(400, default, ex.Message);
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<UsuarioRegistradoDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<UsuarioRegistradoDto>(500, default, "No se pudo completar la operacion.");
        }
    }
}
