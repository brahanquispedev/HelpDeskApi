using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.Usuarios;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.UseCases.IntegrantesEquipo.Queries;

public sealed record ListarPersonasParaEquipoQuery : IRequest<ResponseDto<UsuarioOpcionDto[]>>;

internal sealed class ListarPersonasParaEquipoHandler(IUnitOfWork unitOfWork, AccessRules access)
    : IRequestHandler<ListarPersonasParaEquipoQuery, ResponseDto<UsuarioOpcionDto[]>>
{
    public async Task<ResponseDto<UsuarioOpcionDto[]>> Handle(ListarPersonasParaEquipoQuery request, CancellationToken ct)
    {
        if (!access.Admin) access.Permission("organizacion.administrar");
        var personas = await unitOfWork.Repository<global::HelpDesk.Domain.Models.Usuarios>().Query()
            .Where(usuario => usuario.Activo)
            .OrderBy(usuario => usuario.Apellidos).ThenBy(usuario => usuario.Nombres).ThenBy(usuario => usuario.IdUsuario)
            .Select(usuario => new UsuarioOpcionDto(usuario.IdUsuario, usuario.Nombres + " " + usuario.Apellidos + " · " + usuario.NombreUsuario))
            .ToArrayAsync(ct);
        return new(200, personas);
    }
}
