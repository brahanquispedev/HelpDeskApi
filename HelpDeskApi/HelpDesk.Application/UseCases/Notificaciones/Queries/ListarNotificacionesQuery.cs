using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.Notificaciones;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.UseCases.Notificaciones.Queries;

public class ListarNotificacionesQuery : IRequest<ResponseDto<IEnumerable<NotificacionesResponseDto>>>
{
    public Guid? IdUsuario { get; set; }
    public bool? SoloNoLeidas { get; set; }
}

internal sealed class ListarNotificacionesQueryHandler(IUnitOfWork _unitOfWork, AccessRules access, ICurrentUser actor)
    : IRequestHandler<ListarNotificacionesQuery, ResponseDto<IEnumerable<NotificacionesResponseDto>>>
{
    public async Task<ResponseDto<IEnumerable<NotificacionesResponseDto>>> Handle(ListarNotificacionesQuery request, CancellationToken ct)
    {
        try
        {
            var targetUserId = request.IdUsuario ?? actor.Id;
            if (!access.Admin && targetUserId != actor.Id)
            {
                access.Permission("notificaciones.listar");
            }

            var query = _unitOfWork.Repository<Domain.Models.Notificaciones>().Query()
                .Where(n => n.IdUsuario == targetUserId);

            if (request.SoloNoLeidas.HasValue && request.SoloNoLeidas.Value)
            {
                query = query.Where(n => n.FechaLectura == null);
            }

            var items = await query
                .OrderByDescending(n => n.FechaCreacion)
                .Select(n => new NotificacionesResponseDto
                {
                    IdNotificacion = n.IdNotificacion,
                    IdUsuario = n.IdUsuario,
                    IdTicket = n.IdTicket,
                    Titulo = n.Titulo,
                    Mensaje = n.Mensaje,
                    FechaCreacion = n.FechaCreacion,
                    FechaLectura = n.FechaLectura,
                    ClaveEvento = n.ClaveEvento,
                    TipoEvento = n.TipoEvento
                })
                .ToListAsync(ct);

            return new ResponseDto<IEnumerable<NotificacionesResponseDto>>(200, items, "Notificaciones listadas correctamente.");
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<IEnumerable<NotificacionesResponseDto>>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<IEnumerable<NotificacionesResponseDto>>(500, default, "No se pudo completar la operacion.");
        }
    }
}