using System.ComponentModel.DataAnnotations;
using HelpDesk.Application.Interfaces;
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

internal sealed class CrearNotificacionesCommandHandler(IUnitOfWork _unitOfWork, IAccessRules access)
    : IRequestHandler<CrearNotificacionesCommand, ResponseDto<NotificacionesResponseDto>>
{
    public async Task<ResponseDto<NotificacionesResponseDto>> Handle(CrearNotificacionesCommand request, CancellationToken ct)
    {
        try
        {
            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            if (request.IdUsuario == Guid.Empty)
            {
                throw new ValidationException("IdUsuario debe ser un identificador valido.");
            }
            if (!access.Admin) access.Permission("usuarios.administrar");

            var usuario = await _unitOfWork.Repository<Domain.Models.Usuarios>().GetByIdAsync([request.IdUsuario]);
            if (!(usuario != null))
            {
                throw new BusinessException(400, "El usuario destinatario no existe.");
            }
            if (!(usuario!.Activo))
            {
                throw new BusinessException(400, "El usuario destinatario esta inactivo.");
            }

            if (request.IdTicket.HasValue)
            {
                var ticket = await _unitOfWork.Repository<Domain.Models.Tickets>().GetByIdAsync([request.IdTicket.Value]);
                if (!(ticket != null))
                {
                    throw new BusinessException(400, "El ticket asociado no existe.");
                }
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
        catch (ValidationException ex)
        {
            return new ResponseDto<NotificacionesResponseDto>(400, default, ex.Message);
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