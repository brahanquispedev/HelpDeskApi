namespace HelpDesk.Domain.Dtos.EquiposSoporte;

public class EquiposSoporteResponseDto
{
    public Guid IdEquipo { get; init; }
    public string Nombre { get; init; } = "";
    public string? Descripcion { get; init; }
    public bool Activo { get; init; }
}