using HelpDesk.Application.Services.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.Usuarios;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Models = HelpDesk.Domain.Models;

namespace HelpDesk.Application.UseCases.Tickets.Queries;

public sealed record ListarTecnicosParaTicketQuery(Guid IdTicket) : IRequest<ResponseDto<UsuarioOpcionDto[]>>;

internal sealed class ListarTecnicosParaTicketHandler(IUnitOfWork unitOfWork, AccessRules access)
    : IRequestHandler<ListarTecnicosParaTicketQuery, ResponseDto<UsuarioOpcionDto[]>>
{
    public async Task<ResponseDto<UsuarioOpcionDto[]>> Handle(ListarTecnicosParaTicketQuery request, CancellationToken ct)
    {
        access.Permission("asignaciones.administrar");
        var ticket = await unitOfWork.Repository<Models.Tickets>().GetByIdAsync([request.IdTicket])
            ?? throw new BusinessException(404, "Ticket no encontrado.");
        access.EnsureRead(ticket);

        var integrantes = unitOfWork.Repository<Models.IntegrantesEquipo>().Query();
        if (!access.Admin && !await integrantes.AnyAsync(integrante => integrante.IdEquipo == ticket.IdEquipoAsignado
            && integrante.IdUsuario == access.UserId && integrante.EsCoordinador && integrante.Activo, ct))
            throw new BusinessException(403, "No coordina el equipo de este ticket.");

        var disponibles = integrantes.Where(integrante => integrante.IdEquipo == ticket.IdEquipoAsignado
            && integrante.Activo && integrante.Disponible && integrante.IdEquipoNavigation.Activo)
            .Select(integrante => integrante.IdUsuario);
        var rolesTecnicos = unitOfWork.Repository<Models.Roles>().Query()
            .Where(rol => rol.IdPermiso.Any(permiso => permiso.Codigo == "tickets.atender"))
            .Select(rol => rol.IdRol);
        var tecnicos = unitOfWork.Repository<Models.UsuariosRoles>().Query()
            .Where(relacion => rolesTecnicos.Contains(relacion.IdRol)).Select(relacion => relacion.IdUsuario);
        var opciones = await unitOfWork.Repository<Models.Usuarios>().Query()
            .Where(usuario => usuario.Activo && disponibles.Contains(usuario.IdUsuario) && tecnicos.Contains(usuario.IdUsuario))
            .OrderBy(usuario => usuario.Apellidos).ThenBy(usuario => usuario.Nombres).ThenBy(usuario => usuario.IdUsuario)
            .Select(usuario => new UsuarioOpcionDto(usuario.IdUsuario, usuario.Nombres + " " + usuario.Apellidos + " · " + usuario.NombreUsuario))
            .ToArrayAsync(ct);
        return new(200, opciones);
    }
}
