using HelpDesk.Application.Common;
using HelpDesk.Domain.Dtos.Autenticacion;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace HelpDesk.Application.UseCases.Autenticacion;

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
            Validation.Check(request);
            var nombreUsuario = request.NombreUsuario.Trim();
            var correo = request.Correo.Trim().ToLowerInvariant();

            var existe = await _unitOfWork.Repository<Usuarios>().Query()
                .AnyAsync(u => u.NombreUsuario == nombreUsuario || u.Correo == correo, ct);
            Validation.Require(!existe, "El nombre de usuario o el correo ya estan registrados.", 409);

            var usuario = new Usuarios
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

            await _unitOfWork.Repository<Usuarios>().AddAsync(usuario);
            await _unitOfWork.Complete();

            return new ResponseDto<UsuarioRegistradoDto>(
                201,
                new UsuarioRegistradoDto(usuario.IdUsuario, usuario.NombreUsuario));
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
