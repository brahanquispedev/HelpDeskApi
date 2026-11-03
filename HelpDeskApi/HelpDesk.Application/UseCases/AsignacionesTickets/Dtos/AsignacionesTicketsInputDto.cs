using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Application.UseCases.AsignacionesTickets.Dtos;

public class AsignacionesTicketsInputDto
{
    public Guid IdTicket { get; set; }
    public Guid IdEquipo { get; set; }
    public Guid? IdTecnico { get; set; }
    [Required]
    [StringLength(10000)]
    public string Motivo { get; set; } = "";
}