namespace HelpDesk.Domain.Dtos.Adjuntos;

public class AdjuntosResponseDto
{
    public Guid IdAdjunto { get; set; }
    public Guid IdTicket { get; set; }
    public Guid? IdComentario { get; set; }
    public Guid IdUsuario { get; set; }
    public string NombreArchivo { get; set; } = null!;
    public string TipoArchivo { get; set; } = null!;
    public long TamanoBytes { get; set; }
    public bool EsInterno { get; set; }
    public DateTime Fecha { get; set; }
}