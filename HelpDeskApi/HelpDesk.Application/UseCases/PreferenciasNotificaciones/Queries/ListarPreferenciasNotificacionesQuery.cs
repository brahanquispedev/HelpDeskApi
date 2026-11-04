using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.PreferenciasNotificaciones;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.UseCases.PreferenciasNotificaciones.Queries;

public class ListarPreferenciasNotificacionesQuery : IRequest<ResponseDto<IEnumerable<PreferenciasNotificacionesResponseDto>>>
{
    public Guid? IdUsuario { get; set; }
}

internal sealed class ListarPreferenciasNotificacionesQueryHandler(IUnitOfWork _unitOfWork, AccessRules access, ICurrentUser actor)
    : IRequestHandler<ListarPreferenciasNotificacionesQuery, ResponseDto<IEnumerable<PreferenciasNotificacionesResponseDto>>>
{
    public async Task<ResponseDto<IEnumerable<PreferenciasNotificacionesResponseDto>>> Handle(ListarPreferenciasNotificacionesQuery request, CancellationToken ct)
    {
        try
        {
            var targetUserId = request.IdUsuario ?? actor.Id;
            if (!access.Admin && targetUserId != actor.Id)
            {
                access.Permission("preferencias_notificaciones.listar");
            }

            var items = await _unitOfWork.Repository<Domain.Models.PreferenciasNotificaciones>().Query()
                .Where(p => p.IdUsuario == targetUserId)
                .OrderBy(p => p.TipoEvento)
                .Select(p => new PreferenciasNotificacionesResponseDto
                {
                    IdUsuario = p.IdUsuario,
                    TipoEvento = p.TipoEvento,
                    RecibirEnSistema = p.RecibirEnSistema,
                    RecibirPorCorreo = p.RecibirPorCorreo
                })
                .ToListAsync(ct);

            return new ResponseDto<IEnumerable<PreferenciasNotificacionesResponseDto>>(200, items, "Preferencias de notificaciones listadas correctamente.");
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<IEnumerable<PreferenciasNotificacionesResponseDto>>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<IEnumerable<PreferenciasNotificacionesResponseDto>>(500, default, "No se pudo completar la operacion.");
        }
    }
}