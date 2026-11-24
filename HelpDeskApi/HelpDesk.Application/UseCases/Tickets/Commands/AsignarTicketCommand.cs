using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Dtos.Common;
using Microsoft.EntityFrameworkCore;
using HelpDesk.Application.Services.Security;
using HelpDesk.Application.Services;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
using HelpDesk.Domain.Dtos.Tickets;
namespace HelpDesk.Application.UseCases.Tickets.Commands;

public class AsignarTicketCommand : IRequest<ResponseDto<TicketsResponseDto>>
{
    [JsonIgnore]
    public Guid IdTicket { get; set; }

    [Range(1, int.MaxValue)]
    public int Version { get; set; }
    [Required, StringLength(4000)]
    public string Motivo { get; set; } = string.Empty;
    public Guid? IdEquipo { get; set; }
    public Guid? IdTecnico { get; set; }
}

internal sealed class AsignarTicketCommandHandler(IUnitOfWork _unitOfWork, ICurrentUser actor, IAccessRules access, ITicketNotifications notices)
    : IRequestHandler<AsignarTicketCommand, ResponseDto<TicketsResponseDto>>
{
    public async Task<ResponseDto<TicketsResponseDto>> Handle(AsignarTicketCommand request, CancellationToken ct)
    {
        try
        {
            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            if (request.IdTicket == Guid.Empty)
            {
                throw new ValidationException("IdTicket debe ser un identificador valido.");
            }
            var t = await Required<global::HelpDesk.Domain.Models.Tickets>(request.IdTicket);
            access.EnsureRead(t);
            if (!(t.Version == request.Version))
            {
                throw new BusinessException(409, "El ticket cambio; consulte su version actual.");
            }
            var before = t.Estado;
            var now = DateTime.UtcNow;
            access.Permission("asignaciones.administrar");
            if (!(t.Estado == "nuevo"))
            {
                throw new BusinessException(409, "Solo se asigna un ticket nuevo en esta primera version.");
            }
            var team = request.IdEquipo ?? t.IdEquipoAsignado;
            var tech = request.IdTecnico ?? throw new BusinessException(400, "Indique idTecnico.");
            if (!access.Admin)
                {
                    if (!(_unitOfWork.Repository<global::HelpDesk.Domain.Models.IntegrantesEquipo>().Query().Any(m => m.IdEquipo == team && m.IdUsuario == actor.Id && m.Activo && m.EsCoordinador)))
                    {
                        throw new BusinessException(403, "No coordina el equipo destino.");
                    }
                }
            if (!(_unitOfWork.Repository<global::HelpDesk.Domain.Models.EquiposSoporte>().Query().Any(e => e.IdEquipo == team && e.Activo)))
            {
                throw new BusinessException(400, "Equipo inactivo.");
            }
            if (!(_unitOfWork.Repository<global::HelpDesk.Domain.Models.IntegrantesEquipo>().Query().Any(m => m.IdEquipo == team && m.IdUsuario == tech && m.Activo && m.Disponible)))
            {
                throw new BusinessException(400, "Tecnico no disponible en el equipo.");
            }
            if (!((await Required<global::HelpDesk.Domain.Models.Usuarios>(tech)).Activo))
            {
                throw new BusinessException(400, "Tecnico inactivo.");
            }
            var rolesTecnico = _unitOfWork.Repository<global::HelpDesk.Domain.Models.UsuariosRoles>().Query()
                .Where(relacion => relacion.IdUsuario == tech).Select(relacion => relacion.IdRol);
            if (!(await _unitOfWork.Repository<global::HelpDesk.Domain.Models.Roles>().Query().AnyAsync(rol => rolesTecnico.Contains(rol.IdRol) && rol.IdPermiso.Any(permiso => permiso.Codigo == "tickets.atender"), ct)))
            {
                throw new BusinessException(409, "El integrante no tiene permiso para atender tickets.");
            }
            var asignacion = await _unitOfWork.Repository<global::HelpDesk.Domain.Models.AsignacionesTickets>()
            .Query().SingleOrDefaultAsync(x => x.IdTicket == t.IdTicket && x.FechaFin == null);
            if (asignacion is null)
            {
                asignacion = new global::HelpDesk.Domain.Models.AsignacionesTickets
                {
                    IdAsignacionTicket = Guid.NewGuid(),

                    IdTicket = t.IdTicket,

                    IdAsignador = actor.Id,

                    FechaAsignacion = now
                };
                await _unitOfWork.Repository<global::HelpDesk.Domain.Models.AsignacionesTickets>().AddAsync(asignacion);
            }
            asignacion.IdAsignador = actor.Id;
            asignacion.FechaAsignacion = now;
            asignacion.IdEquipo = team;
            asignacion.IdTecnico = tech;
            asignacion.Motivo = request.Motivo;
            t.IdEquipoAsignado = team;
            t.IdTecnicoAsignado = tech;
            t.Estado = "asignado";
            await History(t, "asignar", before, t.Estado, request.Motivo);
            t.Version++;
            t.FechaActualizacion = now;
            await notices.EnqueueAsync(t, TicketEvent.Assigned, [t.IdSolicitante, tech], ct);
            await _unitOfWork.Complete();
            return new ResponseDto<TicketsResponseDto>(200, new TicketsResponseDto
            {
                IdTicket = t.IdTicket,
                Numero = t.Numero,
                IdSolicitante = t.IdSolicitante,
                IdCreador = t.IdCreador,
                IdAreaSolicitante = t.IdAreaSolicitante,
                IdServicio = t.IdServicio,
                IdEquipoAsignado = t.IdEquipoAsignado,
                IdTecnicoAsignado = t.IdTecnicoAsignado,
                IdPolitica = t.IdPolitica,
                Tipo = t.Tipo,
                Asunto = t.Asunto,
                Descripcion = t.Descripcion,
                Prioridad = t.Prioridad,
                Estado = t.Estado,
                MotivoEspera = t.MotivoEspera,
                Solucion = t.Solucion,
                MotivoCancelacion = t.MotivoCancelacion,
                MinutosPrimeraRespuesta = t.MinutosPrimeraRespuesta,
                MinutosResolucion = t.MinutosResolucion,
                FechaCreacion = t.FechaCreacion,
                FechaActualizacion = t.FechaActualizacion,
                FechaLimiteRespuesta = t.FechaLimiteRespuesta,
                FechaLimiteResolucion = t.FechaLimiteResolucion,
                FechaPrimeraRespuesta = t.FechaPrimeraRespuesta,
                FechaResolucion = t.FechaResolucion,
                FechaCierre = t.FechaCierre,
                FechaCancelacion = t.FechaCancelacion,
                Version = t.Version
            }, "Accion completada.");
        }
        catch (ValidationException ex)
        {
            return new ResponseDto<TicketsResponseDto>(400, default, ex.Message);
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<TicketsResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<TicketsResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }

    private async Task<T> Required<T>(Guid id) where T : class =>
        await _unitOfWork.Repository<T>().GetByIdAsync([id]) ?? throw new BusinessException(404, "Registro relacionado no encontrado.");
    private async Task History(global::HelpDesk.Domain.Models.Tickets ticket, string action, string? before, string? after, string reason) =>
        await _unitOfWork.Repository<global::HelpDesk.Domain.Models.HistorialTickets>().AddAsync(new global::HelpDesk.Domain.Models.HistorialTickets
        {
            IdHistorial = Guid.NewGuid(),

            IdTicket = ticket.IdTicket,

            IdUsuario = actor.Id,

            Accion = action,

            CampoModificado = "estado",

            ValorAnterior = before,

            ValorNuevo = after,

            Motivo = reason,

            Fecha = DateTime.UtcNow
        });
}
