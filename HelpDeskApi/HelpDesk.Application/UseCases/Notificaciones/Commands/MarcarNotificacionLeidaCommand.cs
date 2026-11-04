using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;

namespace HelpDesk.Application.UseCases.Notificaciones.Commands;

public class MarcarNotificacionLeidaCommand : IRequest<ResponseDto<OperacionDto>>
{
    public Guid IdNotificacion { get; set; }

    public MarcarNotificacionLeidaCommand(Guid idNotificacion)
    {
        IdNotificacion = idNotificacion;
    }
}

internal sealed class MarcarNotificacionLeidaCommandHandler(IUnitOfWork _unitOfWork, AccessRules access, ICurrentUser actor)
    : IRequestHandler<MarcarNotificacionLeidaCommand, ResponseDto<OperacionDto>>
{
    public async Task<ResponseDto<OperacionDto>> Handle(MarcarNotificacionLeidaCommand request, CancellationToken ct)
    {
        try
        {
            var repo = _unitOfWork.Repository<Domain.Models.Notificaciones>();
            var entity = await repo.GetByIdAsync([request.IdNotificacion]);
            if (entity == null)
            {
                throw new BusinessException(404, "Notificacion no encontrada.");
            }

            if (!access.Admin && entity.IdUsuario != actor.Id)
            {
                access.Permission("notificaciones.marcar_leida");
            }

            if (!entity.FechaLectura.HasValue)
            {
                entity.FechaLectura = DateTime.UtcNow;
                repo.Update(entity);
                await _unitOfWork.Complete();
            }

            return new ResponseDto<OperacionDto>(200, new OperacionDto(true), "Notificacion marcada como leida.");
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