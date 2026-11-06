namespace HelpDesk.Domain.Dtos.EnviosNotificaciones;

public class EnviosNotificacionesResponseDto
{
    public Guid IdEnvio { get; init; }
    public Guid IdNotificacion { get; init; }
    public string CorreoDestino { get; init; } = "";
    public string Estado { get; init; } = "";
    public int Intentos { get; init; }
    public int MaximoIntentos { get; init; }
    public DateTime FechaCreacion { get; init; }
    public DateTime FechaProximoIntento { get; init; }
    public DateTime? BloqueoHasta { get; init; }
    public DateTime? FechaEnvio { get; init; }
    public string? UltimoError { get; init; }
    public string? IdentificadorProveedor { get; init; }
}