using System.ComponentModel.DataAnnotations;
using HelpDesk.Application.Common;
using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.Notificaciones;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;

namespace HelpDesk.Application.UseCases.Notificaciones.Commands;

public class CrearNotificacionesCommand : IRequest<ResponseDto<NotificacionesResponseDto>>
{
    public Guid IdUsuario { get; set; }
    public Guid? IdTicket { get; set; }

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

internal sealed class CrearNotificacionesCommandHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<CrearNotificacionesCommand, ResponseDto<NotificacionesResponseDto>>
{
    public async Task<ResponseDto<NotificacionesResponseDto>> Handle(CrearNotificacionesCommand request, CancellationToken ct)
    {
        try
        {
            Validation.Check(request);
            if (!access.Admin) access.Permission("notificaciones.crear");

            var usuario = await _unitOfWork.Repository<Domain.Models.Usuarios>().GetByIdAsync([request.IdUsuario]);
            Validation.Require(usuario != null, "El usuario destinatario no existe.");
            Validation.Require(usuario!.Activo, "El usuario destinatario esta inactivo.");

            if (request.IdTicket.HasValue)
            {
                var ticket = await _unitOfWork.Repository<Domain.Models.Tickets>().GetByIdAsync([request.IdTicket.Value]);
                Validation.Require(ticket != null, "El ticket asociado no existe.");
            }

            var entity = new Domain.Models.Notificaciones
            {
                IdNotificacion = Guid.NewGuid(),
                IdUsuario = request.IdUsuario,
                IdTicket = request.IdTicket,
                Titulo = request.Titulo.Trim(),
                Mensaje = request.Mensaje.Trim(),
                FechaCreacion = DateTime.UtcNow,
                FechaLectura = null,
                ClaveEvento = request.ClaveEvento.Trim(),
                TipoEvento = request.TipoEvento.Trim()
            };

            await _unitOfWork.Repository<Domain.Models.Notificaciones>().AddAsync(entity);
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

            return new ResponseDto<NotificacionesResponseDto>(201, dto, "Notificacion creada correctamente.");
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