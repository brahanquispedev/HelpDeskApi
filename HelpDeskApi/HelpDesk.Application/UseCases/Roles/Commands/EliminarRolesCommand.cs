using System.Text.Json.Serialization;
using HelpDesk.Application.Services.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;

namespace HelpDesk.Application.UseCases.Roles.Commands;


public class EliminarRolesCommand : IRequest<ResponseDto<OperacionDto>>
{
    [JsonIgnore]
    public object[] Keys { get; set; } = [];
}

internal sealed class EliminarRolesCommandHandler(IUnitOfWork _unitOfWork, IAccessRules access) : IRequestHandler<EliminarRolesCommand, ResponseDto<OperacionDto>>
{
    public async Task<ResponseDto<OperacionDto>> Handle(EliminarRolesCommand request, CancellationToken ct)
    {
        try
        {
            if (!(access.Admin))
            {
                throw new BusinessException(403, "Esta operacion requiere administracion.");
            }
            var entity = await _unitOfWork.Repository<global::HelpDesk.Domain.Models.Roles>().GetByIdAsync(request.Keys)
                         ?? throw new BusinessException(404, "Registro no encontrado.");
            access.EnsureRead(entity);
            if (!(!new[]
            {
                "administrador",
                "empleado",
                "tecnico",
                "coordinador",
                "aprobador"
            }.Contains(entity.Nombre)))
            {
                throw new BusinessException(409, "Los roles base se conservan.");
            }
            _unitOfWork.Repository<global::HelpDesk.Domain.Models.Roles>(null).Delete(entity);
            await _unitOfWork.Complete();
            return new ResponseDto<OperacionDto>(200, new OperacionDto(true), "Registro eliminado.");
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<OperacionDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<OperacionDto>(500, default, "No se pudo completar la operacion.");
        }
    }

}