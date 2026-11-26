using HelpDesk.Domain.Models;

namespace HelpDesk.Application.Interfaces;

public interface ITicketNotifications
{
    Task NotifyCoordinatorsAsync(Tickets ticket, CancellationToken ct);
    Task EnqueueAsync(Tickets ticket, TicketEvent kind,
        IEnumerable<Guid?> recipients, CancellationToken ct,
        Guid? commentId = null);
}
