namespace HelpDesk.Domain.Dtos.Notificaciones;

public class NotificacionesResponseDto
{
    public Guid IdNotificacion { get; init; }
    public Guid IdUsuario { get; init; }
    public Guid? IdTicket { get; init; }
    public string Titulo { get; init; } = "";
    public string Mensaje { get; init; } = "";
    public DateTime FechaCreacion { get; init; }
    public DateTime? FechaLectura { get; init; }
    public string ClaveEvento { get; init; } = "";
    public string TipoEvento { get; init; } = "";
}