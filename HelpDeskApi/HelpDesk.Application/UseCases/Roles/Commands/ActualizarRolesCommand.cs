using HelpDesk.Domain.Dtos.Roles;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HelpDesk.Application.Services.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;

namespace HelpDesk.Application.UseCases.Roles.Commands;

public class ActualizarRolesCommand : IRequest<ResponseDto<RolesResponseDto>>
{
    [JsonIgnore]
    public object[] Keys { get; set; } = [];

    [Required]
    [StringLength(10000)]
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
}

internal sealed class ActualizarRolesCommandHandler(IUnitOfWork _unitOfWork, IAccessRules access) : IRequestHandler<ActualizarRolesCommand, ResponseDto<RolesResponseDto>>
{
    public async Task<ResponseDto<RolesResponseDto>> Handle(ActualizarRolesCommand request, CancellationToken ct)
    {
        try
        {

            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            if (!(access.Admin))
            {
                throw new BusinessException(403, "Esta operacion requiere administracion.");
            }
            var entity = await _unitOfWork.Repository<global::HelpDesk.Domain.Models.Roles>().GetByIdAsync(request.Keys)
                         ?? throw new BusinessException(404, "Registro no encontrado.");
            access.EnsureRead(entity);
            if (new[] { "administrador", "empleado", "tecnico", "coordinador", "aprobador" }.Contains(entity.Nombre))
                {
                    if (!(request.Nombre == entity.Nombre))
                    {
                        throw new BusinessException(409, "No se renombra un rol base.");
                    }
                }
            entity.Nombre = request.Nombre;
            entity.Descripcion = request.Descripcion;

            await _unitOfWork.Complete();
            return new ResponseDto<RolesResponseDto>(200, new RolesResponseDto
            {
                IdRol = entity.IdRol,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion
            }, "Registro actualizado.");
        }
        catch (ValidationException ex)
        {
            return new ResponseDto<RolesResponseDto>(400, default, ex.Message);
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<RolesResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<RolesResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }

}