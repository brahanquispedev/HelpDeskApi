namespace HelpDesk.Domain.Dtos.IntegrantesEquipo;

public class IntegrantesEquipoResponseDto
{
    public Guid IdEquipo { get; init; }
    public Guid IdUsuario { get; init; }
    public bool EsCoordinador { get; init; }
    public bool Disponible { get; init; }
    public bool Activo { get; init; }
}