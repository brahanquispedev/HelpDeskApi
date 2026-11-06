using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.EnviosNotificaciones;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.UseCases.EnviosNotificaciones.Queries;

public class ListarEnviosNotificacionesQuery : IRequest<ResponseDto<IEnumerable<EnviosNotificacionesResponseDto>>>
{
    public Guid? IdNotificacion { get; set; }
    public string? Estado { get; set; }
}

internal sealed class ListarEnviosNotificacionesQueryHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<ListarEnviosNotificacionesQuery, ResponseDto<IEnumerable<EnviosNotificacionesResponseDto>>>
{
    public async Task<ResponseDto<IEnumerable<EnviosNotificacionesResponseDto>>> Handle(ListarEnviosNotificacionesQuery request, CancellationToken ct)
    {
        try
        {
            if (!access.Admin) access.Permission("envios_notificaciones.listar");

            var query = _unitOfWork.Repository<Domain.Models.EnviosNotificaciones>().Query();

            if (request.IdNotificacion.HasValue)
            {
                query = query.Where(e => e.IdNotificacion == request.IdNotificacion.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Estado))
            {
                var estadoNorm = request.Estado.Trim().ToLower();
                query = query.Where(e => e.Estado.ToLower() == estadoNorm);
            }

            var items = await query
                .OrderByDescending(e => e.FechaCreacion)
                .Select(e => new EnviosNotificacionesResponseDto
                {
                    IdEnvio = e.IdEnvio,
                    IdNotificacion = e.IdNotificacion,
                    CorreoDestino = e.CorreoDestino,
                    Estado = e.Estado,
                    Intentos = e.Intentos,
                    MaximoIntentos = e.MaximoIntentos,
                    FechaCreacion = e.FechaCreacion,
                    FechaProximoIntento = e.FechaProximoIntento,
                    BloqueoHasta = e.BloqueoHasta,
                    FechaEnvio = e.FechaEnvio,
                    UltimoError = e.UltimoError,
                    IdentificadorProveedor = e.IdentificadorProveedor
                })
                .ToListAsync(ct);

            return new ResponseDto<IEnumerable<EnviosNotificacionesResponseDto>>(200, items, "Envios de notificaciones listados correctamente.");
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<IEnumerable<EnviosNotificacionesResponseDto>>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<IEnumerable<EnviosNotificacionesResponseDto>>(500, default, "No se pudo completar la operacion.");
        }
    }
}