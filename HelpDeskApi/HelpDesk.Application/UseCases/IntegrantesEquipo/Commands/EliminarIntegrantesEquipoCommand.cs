using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;

namespace HelpDesk.Application.UseCases.IntegrantesEquipo.Commands;

public class EliminarIntegrantesEquipoCommand : IRequest<ResponseDto<OperacionDto>>
{
    public Guid IdEquipo { get; set; }
    public Guid IdUsuario { get; set; }

    public EliminarIntegrantesEquipoCommand(Guid idEquipo, Guid idUsuario)
    {
        IdEquipo = idEquipo;
        IdUsuario = idUsuario;
    }
}

internal sealed class EliminarIntegrantesEquipoCommandHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<EliminarIntegrantesEquipoCommand, ResponseDto<OperacionDto>>
{
    public async Task<ResponseDto<OperacionDto>> Handle(EliminarIntegrantesEquipoCommand request, CancellationToken ct)
    {
        try
        {
            if (!access.Admin) access.Permission("integrantes_equipo.eliminar");

            var repo = _unitOfWork.Repository<Domain.Models.IntegrantesEquipo>();
            var entity = await repo.GetByIdAsync([request.IdEquipo, request.IdUsuario]);
            if (entity == null)
            {
                throw new BusinessException(404, "Integrante de equipo no encontrado.");
            }

            entity.Activo = false;
            repo.Update(entity);
            await _unitOfWork.Complete();

            return new ResponseDto<OperacionDto>(200, new OperacionDto(true), "Integrante de equipo desactivado correctamente.");
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