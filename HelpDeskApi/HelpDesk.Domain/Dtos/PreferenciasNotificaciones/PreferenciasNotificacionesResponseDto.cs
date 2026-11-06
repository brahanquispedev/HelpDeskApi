namespace HelpDesk.Domain.Dtos.PreferenciasNotificaciones;

public class PreferenciasNotificacionesResponseDto
{
    public Guid IdUsuario { get; init; }
    public string TipoEvento { get; init; } = "";
    public bool RecibirEnSistema { get; init; }
    public bool RecibirPorCorreo { get; init; }
}