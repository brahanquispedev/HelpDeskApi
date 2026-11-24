using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using HelpDesk.Domain.Dtos.Autenticacion;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.UseCases.Autenticacion.Commands;

public sealed class IniciarSesionCommand : IRequest<ResponseDto<SesionDto>>
{
    [Required, StringLength(254)]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 10)]
    public string Clave { get; set; } = string.Empty;
}

internal sealed class IniciarSesionCommandHandler(
    IUnitOfWork _unitOfWork,
    IPasswordService _passwordService,
    ITokenService _tokenService)
    : IRequestHandler<IniciarSesionCommand, ResponseDto<SesionDto>>
{
    public async Task<ResponseDto<SesionDto>> Handle(IniciarSesionCommand request, CancellationToken ct)
    {
        try
        {
            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            var nombreUsuario = request.NombreUsuario.Trim();
            var correo = nombreUsuario.ToLowerInvariant();

            var usuario = await _unitOfWork.Repository<global::HelpDesk.Domain.Models.Usuarios>().Query()
                .FirstOrDefaultAsync(u => u.NombreUsuario.ToLower() == correo || u.Correo == correo, ct);

            if (!(usuario is not null))
            {
                throw new BusinessException(401, "Usuario o clave incorrectos.");
            }
            if (!(usuario!.Activo))
            {
                throw new BusinessException(403, "Usuario inactivo.");
            }
            if (!(_passwordService.Verify(usuario.ClaveHash, request.Clave)))
            {
                throw new BusinessException(401, "Usuario o clave incorrectos.");
            }

            var roleIds = await _unitOfWork.Repository<global::HelpDesk.Domain.Models.UsuariosRoles>().Query()
                .Where(x => x.IdUsuario == usuario.IdUsuario)
                .Select(x => x.IdRol)
                .ToListAsync(ct);

            var roles = await _unitOfWork.Repository<global::HelpDesk.Domain.Models.Roles>().Query()
                .Where(r => roleIds.Contains(r.IdRol))
                .Select(r => r.Nombre)
                .ToListAsync(ct);

            var credentialStamp = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(usuario.ClaveHash)));
            var token = _tokenService.Create(usuario.IdUsuario, usuario.NombreUsuario, roles, credentialStamp);

            return new ResponseDto<SesionDto>(
                200,
                new SesionDto(token.Token, token.ExpiresAt, usuario.IdUsuario, usuario.NombreUsuario, roles));
        }
        catch (ValidationException ex)
        {
            return new ResponseDto<SesionDto>(400, default, ex.Message);
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<SesionDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<SesionDto>(500, default, "No se pudo completar la operacion.");
        }
    }
}
