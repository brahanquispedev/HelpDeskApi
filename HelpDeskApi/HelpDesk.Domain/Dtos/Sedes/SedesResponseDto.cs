namespace HelpDesk.Domain.Dtos.Sedes;

public class SedesResponseDto
{
    public Guid IdSede { get; init; }
    public string Nombre { get; init; } = "";
    public string? Direccion { get; init; }
    public bool Activo { get; init; }
}