using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Application.Services.Security;
using HelpDesk.Domain.Dtos.Usuarios;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
namespace HelpDesk.Application.UseCases.Usuarios.Commands;

public class CrearUsuariosCommand : IRequest<ResponseDto<UsuariosResponseDto>>
{
    public Guid? IdArea { get; set; }
    [Required]
    [StringLength(10000)]
    public string Nombres { get; set; } = "";
    [Required]
    [StringLength(10000)]
    public string Apellidos { get; set; } = "";
    [Required]
    [StringLength(10000)]
    public string NombreUsuario { get; set; } = "";
    [Required, EmailAddress, StringLength(254)]
    public string Correo { get; set; } = "";
    public string? Telefono { get; set; }
    public bool Activo { get; set; } = true;
    [StringLength(128, MinimumLength = 10)]
    public string? Clave { get; set; }
}

internal sealed class CrearUsuariosCommandHandler(IUnitOfWork _unitOfWork, AccessRules access, IPasswordService passwords) : IRequestHandler<CrearUsuariosCommand, ResponseDto<UsuariosResponseDto>>
{
    public async Task<ResponseDto<UsuariosResponseDto>> Handle(CrearUsuariosCommand request, CancellationToken ct)
    {
        try
        {

            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            if (!(access.Admin))
            {
                throw new BusinessException(403, "Esta operacion requiere administracion.");
            }
            if (request.IdArea is Guid idArea)
            {
                var area = await _unitOfWork.Repository<global::HelpDesk.Domain.Models.Areas>().GetByIdAsync([idArea])
                    ?? throw new BusinessException(400, "El area no existe.");
                if (!(area.Activo))
                {
                    throw new BusinessException(400, "El area esta inactiva.");
                }
            }
            var entity = new global::HelpDesk.Domain.Models.Usuarios()
            {
                IdUsuario = Guid.NewGuid(),
                IdArea = request.IdArea,
                Nombres = request.Nombres,
                Apellidos = request.Apellidos,
                NombreUsuario = request.NombreUsuario,
                Correo = request.Correo,
                Telefono = request.Telefono,
                Activo = request.Activo,
                FechaRegistro = DateTime.UtcNow
            };
            var userInput = request;
            entity.NombreUsuario = entity.NombreUsuario.Trim();
            entity.Correo = entity.Correo.Trim().ToLowerInvariant();
            if (!(!string.IsNullOrWhiteSpace(userInput.Clave)))
            {
                throw new BusinessException(400, "Clave es obligatoria al crear un usuario.");
            }
            if (!string.IsNullOrWhiteSpace(userInput.Clave)) entity.ClaveHash = passwords.Hash(userInput.Clave);

            await _unitOfWork.Repository<global::HelpDesk.Domain.Models.Usuarios>().AddAsync(entity);
            await _unitOfWork.Complete();
            return new ResponseDto<UsuariosResponseDto>(201, new UsuariosResponseDto
            {
                IdUsuario = entity.IdUsuario,
                IdArea = entity.IdArea,
                Nombres = entity.Nombres,
                Apellidos = entity.Apellidos,
                NombreUsuario = entity.NombreUsuario,
                Correo = entity.Correo,
                Telefono = entity.Telefono,
                Activo = entity.Activo,
                FechaRegistro = entity.FechaRegistro
            }, "Registro creado.");
        }
        catch (ValidationException ex)
        {
            return new ResponseDto<UsuariosResponseDto>(400, default, ex.Message);
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<UsuariosResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<UsuariosResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }

}
