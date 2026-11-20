using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.Services;

internal enum TicketEvent { Created, Assigned, Started, Resolved, Closed, PublicComment }

internal sealed class TicketNotifications(IUnitOfWork unitOfWork, ICurrentUser actor)
{
    public async Task NotifyCoordinatorsAsync(Tickets ticket, CancellationToken ct)
    {
        var authorizedRoles = unitOfWork.Repository<Roles>().Query()
            .Where(role => role.IdPermiso.Any(permission => permission.Codigo == "asignaciones.administrar"))
            .Select(role => role.IdRol);
        var authorizedUsers = unitOfWork.Repository<UsuariosRoles>().Query()
            .Where(assignment => authorizedRoles.Contains(assignment.IdRol)).Select(assignment => assignment.IdUsuario);
        var coordinators = await unitOfWork.Repository<IntegrantesEquipo>().Query()
            .Where(member => member.IdEquipo == ticket.IdEquipoAsignado && member.Activo
                && member.EsCoordinador && authorizedUsers.Contains(member.IdUsuario))
            .Select(member => (Guid?)member.IdUsuario).ToArrayAsync(ct);
        await EnqueueAsync(ticket, TicketEvent.Created, coordinators, ct);
    }

    public async Task EnqueueAsync(Tickets ticket, TicketEvent kind, IEnumerable<Guid?> recipients,
        CancellationToken ct, Guid? commentId = null)
    {
        var ids = recipients.Where(id => id.HasValue && id != actor.Id).Select(id => id!.Value).Distinct().ToArray();
        if (ids.Length == 0) return;

        var (type, title, message) = kind switch
        {
            TicketEvent.Created => ("ticket_creado", "Nuevo ticket para tu equipo", "Hay una nueva solicitud pendiente de asignación."),
            TicketEvent.Assigned => ("ticket_asignado", "Ticket asignado", "Se asignó el ticket a un técnico."),
            TicketEvent.Started => ("ticket_en_atencion", "Atención iniciada", "El equipo de soporte empezó a atender el ticket."),
            TicketEvent.Resolved => ("ticket_resuelto", "Ticket resuelto", "Revisa la solución y confirma el cierre cuando corresponda."),
            TicketEvent.Closed => ("ticket_cerrado", "Ticket cerrado", "El solicitante confirmó el cierre."),
            TicketEvent.PublicComment => ("ticket_comentario", "Nuevo comentario", "Hay un comentario nuevo en el ticket."),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        var key = commentId.HasValue
            ? $"ticket:{ticket.IdTicket:N}:comentario:{commentId.Value:N}"
            : $"ticket:{ticket.IdTicket:N}:{ticket.Version}:{type}";

        var optedOut = unitOfWork.Repository<PreferenciasNotificaciones>().Query()
            .Where(preference => preference.TipoEvento == type && !preference.RecibirEnSistema)
            .Select(preference => preference.IdUsuario);
        var eligible = await unitOfWork.Repository<Usuarios>().Query()
            .Where(user => user.Activo && ids.Contains(user.IdUsuario) && !optedOut.Contains(user.IdUsuario))
            .Select(user => user.IdUsuario).ToArrayAsync(ct);
        var notifications = unitOfWork.Repository<Notificaciones>();
        var now = DateTime.UtcNow;
        foreach (var id in eligible)
            await notifications.AddAsync(new Notificaciones
            {
                IdNotificacion = Guid.NewGuid(), IdUsuario = id, IdTicket = ticket.IdTicket,
                TipoEvento = type, ClaveEvento = key, Titulo = title, Mensaje = message, FechaCreacion = now
            });
    }
}
