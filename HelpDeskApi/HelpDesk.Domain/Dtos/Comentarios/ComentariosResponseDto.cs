namespace HelpDesk.Domain.Dtos.Comentarios;

public class ComentariosResponseDto
{
    public Guid IdComentario { get; set; }
    public Guid IdTicket { get; set; }
    public Guid IdAutor { get; set; }      // Actualizado de IdUsuario a IdAutor
    public string Mensaje { get; set; } = null!; // Actualizado de Texto a Mensaje
    public bool EsInterno { get; set; }
    public DateTime Fecha { get; set; }
}