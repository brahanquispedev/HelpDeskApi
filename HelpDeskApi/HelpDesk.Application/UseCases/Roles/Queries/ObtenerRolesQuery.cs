using HelpDesk.Domain.Dtos.Roles;
using HelpDesk.Application.Services.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.UseCases.Roles.Queries;

public class ObtenerRolesQuery : IRequest<ResponseDto<RolesResponseDto>>
{
    public Guid IdRol { get; set; }
}

internal sealed class ObtenerRolesQueryHandler(IUnitOfWork _unitOfWork, IAccessRules access) : IRequestHandler<ObtenerRolesQuery, ResponseDto<RolesResponseDto>>
{
    public async Task<ResponseDto<RolesResponseDto>> Handle(ObtenerRolesQuery request, CancellationToken ct)
    {
        try
        {
            var entity = await _unitOfWork.Repository<global::HelpDesk.Domain.Models.Roles>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdRol == request.IdRol)
                         ?? throw new BusinessException(404, "Registro no encontrado.");
            access.EnsureRead(entity);
            return new ResponseDto<RolesResponseDto>(200, new RolesResponseDto
            {
                IdRol = entity.IdRol,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion
            });
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