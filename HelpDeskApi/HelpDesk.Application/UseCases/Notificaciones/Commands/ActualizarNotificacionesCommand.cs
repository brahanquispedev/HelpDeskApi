using System.ComponentModel.DataAnnotations;
using HelpDesk.Application.Common;
using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.Notificaciones;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;

namespace HelpDesk.Application.UseCases.Notificaciones.Commands;

public class ActualizarNotificacionesCommand : IRequest<ResponseDto<NotificacionesResponseDto>>
{
    public Guid IdNotificacion { get; set; }

    [Required]
    [StringLength(200)]
    public string Titulo { get; set; } = "";

    [Required]
    [StringLength(2000)]
    public string Mensaje { get; set; } = "";

    [Required]
    [StringLength(100)]
    public string ClaveEvento { get; set; } = "";

    [Required]
    [StringLength(50)]
    public string TipoEvento { get; set; } = "";
}

internal sealed class ActualizarNotificacionesCommandHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<ActualizarNotificacionesCommand, ResponseDto<NotificacionesResponseDto>>
{
    public async Task<ResponseDto<NotificacionesResponseDto>> Handle(ActualizarNotificacionesCommand request, CancellationToken ct)
    {
        try
        {
            Validation.Check(request);
            if (!access.Admin) access.Permission("notificaciones.actualizar");

            var repo = _unitOfWork.Repository<Domain.Models.Notificaciones>();
            var entity = await repo.GetByIdAsync([request.IdNotificacion]);
            if (entity == null)
            {
                throw new BusinessException(404, "Notificacion no encontrada.");
            }

            entity.Titulo = request.Titulo.Trim();
            entity.Mensaje = request.Mensaje.Trim();
            entity.ClaveEvento = request.ClaveEvento.Trim();
            entity.TipoEvento = request.TipoEvento.Trim();

            repo.Update(entity);
            await _unitOfWork.Complete();

            var dto = new NotificacionesResponseDto
            {
                IdNotificacion = entity.IdNotificacion,
                IdUsuario = entity.IdUsuario,
                IdTicket = entity.IdTicket,
                Titulo = entity.Titulo,
                Mensaje = entity.Mensaje,
                FechaCreacion = entity.FechaCreacion,
                FechaLectura = entity.FechaLectura,
                ClaveEvento = entity.ClaveEvento,
                TipoEvento = entity.TipoEvento
            };

            return new ResponseDto<NotificacionesResponseDto>(200, dto, "Notificacion actualizada correctamente.");
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<NotificacionesResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<NotificacionesResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }
}