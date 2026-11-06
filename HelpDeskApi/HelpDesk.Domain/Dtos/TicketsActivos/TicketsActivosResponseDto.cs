namespace HelpDesk.Domain.Dtos.TicketsActivos;

public class TicketsActivosResponseDto
{
    public Guid IdTicketActivo { get; set; }
    public Guid IdTicket { get; set; }
    public Guid IdActivo { get; set; }
    public DateTime FechaAsignacion { get; set; }
}