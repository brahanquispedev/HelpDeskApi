using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;

namespace HelpDesk.Application.UseCases.PreferenciasNotificaciones.Commands;

public class EliminarPreferenciasNotificacionesCommand : IRequest<ResponseDto<OperacionDto>>
{
    public Guid IdUsuario { get; set; }
    public string TipoEvento { get; set; }

    public EliminarPreferenciasNotificacionesCommand(Guid idUsuario, string tipoEvento)
    {
        IdUsuario = idUsuario;
        TipoEvento = tipoEvento;
    }
}

internal sealed class EliminarPreferenciasNotificacionesCommandHandler(IUnitOfWork _unitOfWork, AccessRules access, ICurrentUser actor)
    : IRequestHandler<EliminarPreferenciasNotificacionesCommand, ResponseDto<OperacionDto>>
{
    public async Task<ResponseDto<OperacionDto>> Handle(EliminarPreferenciasNotificacionesCommand request, CancellationToken ct)
    {
        try
        {
            if (!access.Admin && request.IdUsuario != actor.Id)
            {
                access.Permission("preferencias_notificaciones.eliminar");
            }

            var repo = _unitOfWork.Repository<Domain.Models.PreferenciasNotificaciones>();
            var entity = await repo.GetByIdAsync([request.IdUsuario, request.TipoEvento.Trim()]);
            if (entity == null)
            {
                throw new BusinessException(404, "Preferencia de notificacion no encontrada.");
            }

            // Al ser una tabla de configuración de usuario, el delete elimina la personalización
            repo.Delete(entity);
            await _unitOfWork.Complete();

            return new ResponseDto<OperacionDto>(200, new OperacionDto(true), "Preferencia eliminada correctamente.");
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