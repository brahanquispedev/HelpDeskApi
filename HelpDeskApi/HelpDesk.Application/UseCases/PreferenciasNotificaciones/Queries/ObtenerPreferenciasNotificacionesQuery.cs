using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.PreferenciasNotificaciones;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;

namespace HelpDesk.Application.UseCases.PreferenciasNotificaciones.Queries;

public class ObtenerPreferenciasNotificacionesQuery : IRequest<ResponseDto<PreferenciasNotificacionesResponseDto>>
{
    public Guid IdUsuario { get; set; }
    public string TipoEvento { get; set; }

    public ObtenerPreferenciasNotificacionesQuery(Guid idUsuario, string tipoEvento)
    {
        IdUsuario = idUsuario;
        TipoEvento = tipoEvento;
    }
}

internal sealed class ObtenerPreferenciasNotificacionesQueryHandler(IUnitOfWork _unitOfWork, AccessRules access, ICurrentUser actor)
    : IRequestHandler<ObtenerPreferenciasNotificacionesQuery, ResponseDto<PreferenciasNotificacionesResponseDto>>
{
    public async Task<ResponseDto<PreferenciasNotificacionesResponseDto>> Handle(ObtenerPreferenciasNotificacionesQuery request, CancellationToken ct)
    {
        try
        {
            if (!access.Admin && request.IdUsuario != actor.Id)
            {
                access.Permission("preferencias_notificaciones.obtener");
            }

            var pref = await _unitOfWork.Repository<Domain.Models.PreferenciasNotificaciones>().GetByIdAsync([request.IdUsuario, request.TipoEvento]);
            if (pref == null)
            {
                throw new BusinessException(404, "Preferencia de notificacion no encontrada.");
            }

            var dto = new PreferenciasNotificacionesResponseDto
            {
                IdUsuario = pref.IdUsuario,
                TipoEvento = pref.TipoEvento,
                RecibirEnSistema = pref.RecibirEnSistema,
                RecibirPorCorreo = pref.RecibirPorCorreo
            };

            return new ResponseDto<PreferenciasNotificacionesResponseDto>(200, dto, "Preferencia obtenida correctamente.");
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<PreferenciasNotificacionesResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<PreferenciasNotificacionesResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }
}