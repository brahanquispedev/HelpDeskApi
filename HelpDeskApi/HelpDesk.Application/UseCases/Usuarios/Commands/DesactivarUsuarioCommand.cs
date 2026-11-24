using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Dtos.Common;
using Microsoft.EntityFrameworkCore;
using HelpDesk.Application.Services.Security;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
namespace HelpDesk.Application.UseCases.Usuarios.Commands;

public class DesactivarUsuarioCommand : IRequest<ResponseDto<AccionResultadoDto>>
{
    [JsonIgnore]
    public Guid Id { get; set; }

    [StringLength(4000)]
    public string Motivo { get; set; } = string.Empty;
}

internal sealed class DesactivarUsuarioCommandHandler(IUnitOfWork _unitOfWork, IAccessRules access)
    : IRequestHandler<DesactivarUsuarioCommand, ResponseDto<AccionResultadoDto>>
{
    public async Task<ResponseDto<AccionResultadoDto>> Handle(DesactivarUsuarioCommand r, CancellationToken ct)
    {
        try
        {
            Validator.ValidateObject(r, new ValidationContext(r), validateAllProperties: true);
            if (r.Id == Guid.Empty)
            {
                throw new ValidationException("Id debe ser un identificador valido.");
            }
            var result = "completado";
            access.Permission("usuarios.administrar");
            var user = await Required<global::HelpDesk.Domain.Models.Usuarios>(r.Id);
            await EnsureNotLastAdmin(user.IdUsuario);
            user.Activo = false;
            result = user.Activo ? "activo" : "inactivo";
            await _unitOfWork.Complete();
            return new ResponseDto<AccionResultadoDto>(200, new AccionResultadoDto(r.Id, result));
        }
        catch (ValidationException ex)
        {
            return new ResponseDto<AccionResultadoDto>(400, default, ex.Message);
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<AccionResultadoDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<AccionResultadoDto>(500, default, "No se pudo completar la operacion.");
        }
    }

    private async Task<T> Required<T>(Guid id) where T : class =>
        await _unitOfWork.Repository<T>().GetByIdAsync([id]) ?? throw new BusinessException(404, "Registro relacionado no encontrado.");
    private async Task EnsureNotLastAdmin(Guid userId)
    {
        var roleIds = _unitOfWork.Repository<global::HelpDesk.Domain.Models.Roles>().Query().Where(r => r.Nombre == "administrador").Select(r => r.IdRol);
        if (!_unitOfWork.Repository<global::HelpDesk.Domain.Models.UsuariosRoles>().Query().Any(r => r.IdUsuario == userId && roleIds.Contains(r.IdRol))) return;
        var others = _unitOfWork.Repository<global::HelpDesk.Domain.Models.UsuariosRoles>().Query().Where(r => roleIds.Contains(r.IdRol) && r.IdUsuario != userId).Select(r => r.IdUsuario);
        if (!(await(_unitOfWork.Repository<global::HelpDesk.Domain.Models.Usuarios>().Query().Where(u => u.Activo && others.Contains(u.IdUsuario))).CountAsync() > 0))
        {
            throw new BusinessException(409, "Debe conservarse al menos un administrador activo.");
        }
    }
}