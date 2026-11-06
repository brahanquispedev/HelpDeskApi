using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.Notificaciones;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;

namespace HelpDesk.Application.UseCases.Notificaciones.Queries;

public class ObtenerNotificacionesQuery : IRequest<ResponseDto<NotificacionesResponseDto>>
{
    public Guid IdNotificacion { get; set; }

    public ObtenerNotificacionesQuery(Guid idNotificacion)
    {
        IdNotificacion = idNotificacion;
    }
}

internal sealed class ObtenerNotificacionesQueryHandler(IUnitOfWork _unitOfWork, AccessRules access, ICurrentUser actor)
    : IRequestHandler<ObtenerNotificacionesQuery, ResponseDto<NotificacionesResponseDto>>
{
    public async Task<ResponseDto<NotificacionesResponseDto>> Handle(ObtenerNotificacionesQuery request, CancellationToken ct)
    {
        try
        {
            var notificacion = await _unitOfWork.Repository<Domain.Models.Notificaciones>().GetByIdAsync([request.IdNotificacion]);
            if (notificacion == null)
            {
                throw new BusinessException(404, "Notificacion no encontrada.");
            }

            if (!access.Admin && notificacion.IdUsuario != actor.Id)
            {
                access.Permission("notificaciones.obtener");
            }

            var dto = new NotificacionesResponseDto
            {
                IdNotificacion = notificacion.IdNotificacion,
                IdUsuario = notificacion.IdUsuario,
                IdTicket = notificacion.IdTicket,
                Titulo = notificacion.Titulo,
                Mensaje = notificacion.Mensaje,
                FechaCreacion = notificacion.FechaCreacion,
                FechaLectura = notificacion.FechaLectura,
                ClaveEvento = notificacion.ClaveEvento,
                TipoEvento = notificacion.TipoEvento
            };

            return new ResponseDto<NotificacionesResponseDto>(200, dto, "Notificacion obtenida correctamente.");
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<NotificacionesResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<NotificacionesResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }
}