using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.EnviosNotificaciones;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;

namespace HelpDesk.Application.UseCases.EnviosNotificaciones.Queries;

public class ObtenerEnviosNotificacionesQuery : IRequest<ResponseDto<EnviosNotificacionesResponseDto>>
{
    public Guid IdEnvio { get; set; }

    public ObtenerEnviosNotificacionesQuery(Guid idEnvio)
    {
        IdEnvio = idEnvio;
    }
}

internal sealed class ObtenerEnviosNotificacionesQueryHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<ObtenerEnviosNotificacionesQuery, ResponseDto<EnviosNotificacionesResponseDto>>
{
    public async Task<ResponseDto<EnviosNotificacionesResponseDto>> Handle(ObtenerEnviosNotificacionesQuery request, CancellationToken ct)
    {
        try
        {
            if (!access.Admin) access.Permission("envios_notificaciones.obtener");

            var entity = await _unitOfWork.Repository<Domain.Models.EnviosNotificaciones>().GetByIdAsync([request.IdEnvio]);
            if (entity == null)
            {
                throw new BusinessException(404, "Envio de notificacion no encontrado.");
            }

            var dto = new EnviosNotificacionesResponseDto
            {
                IdEnvio = entity.IdEnvio,
                IdNotificacion = entity.IdNotificacion,
                CorreoDestino = entity.CorreoDestino,
                Estado = entity.Estado,
                Intentos = entity.Intentos,
                MaximoIntentos = entity.MaximoIntentos,
                FechaCreacion = entity.FechaCreacion,
                FechaProximoIntento = entity.FechaProximoIntento,
                BloqueoHasta = entity.BloqueoHasta,
                FechaEnvio = entity.FechaEnvio,
                UltimoError = entity.UltimoError,
                IdentificadorProveedor = entity.IdentificadorProveedor
            };

            return new ResponseDto<EnviosNotificacionesResponseDto>(200, dto, "Envio de notificacion obtenido correctamente.");
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<EnviosNotificacionesResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<EnviosNotificacionesResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }
}