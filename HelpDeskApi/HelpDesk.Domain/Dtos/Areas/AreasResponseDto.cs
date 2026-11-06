namespace HelpDesk.Domain.Dtos.Areas;

public class AreasResponseDto
{
    public Guid IdArea { get; init; }
    public Guid IdSede { get; init; }
    public string Nombre { get; init; } = "";
    public bool Activo { get; init; }
}