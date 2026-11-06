using System.ComponentModel.DataAnnotations;
using HelpDesk.Application.Common;
using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.PreferenciasNotificaciones;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;

namespace HelpDesk.Application.UseCases.PreferenciasNotificaciones.Commands;

public class CrearPreferenciasNotificacionesCommand : IRequest<ResponseDto<PreferenciasNotificacionesResponseDto>>
{
    public Guid IdUsuario { get; set; }

    [Required]
    [StringLength(50)]
    public string TipoEvento { get; set; } = "";

    public bool RecibirEnSistema { get; set; } = true;
    public bool RecibirPorCorreo { get; set; } = true;
}

internal sealed class CrearPreferenciasNotificacionesCommandHandler(IUnitOfWork _unitOfWork, AccessRules access, ICurrentUser actor)
    : IRequestHandler<CrearPreferenciasNotificacionesCommand, ResponseDto<PreferenciasNotificacionesResponseDto>>
{
    public async Task<ResponseDto<PreferenciasNotificacionesResponseDto>> Handle(CrearPreferenciasNotificacionesCommand request, CancellationToken ct)
    {
        try
        {
            Validation.Check(request);
            if (!access.Admin && request.IdUsuario != actor.Id)
            {
                access.Permission("preferencias_notificaciones.crear");
            }

            var usuario = await _unitOfWork.Repository<Domain.Models.Usuarios>().GetByIdAsync([request.IdUsuario]);
            Validation.Require(usuario != null, "El usuario especificado no existe.");

            var repo = _unitOfWork.Repository<Domain.Models.PreferenciasNotificaciones>();
            var existente = await repo.GetByIdAsync([request.IdUsuario, request.TipoEvento.Trim()]);
            Validation.Require(existente == null, "La preferencia para este tipo de evento ya está configurada.");

            var entity = new Domain.Models.PreferenciasNotificaciones
            {
                IdUsuario = request.IdUsuario,
                TipoEvento = request.TipoEvento.Trim(),
                RecibirEnSistema = request.RecibirEnSistema,
                RecibirPorCorreo = request.RecibirPorCorreo
            };

            await repo.AddAsync(entity);
            await _unitOfWork.Complete();

            var dto = new PreferenciasNotificacionesResponseDto
            {
                IdUsuario = entity.IdUsuario,
                TipoEvento = entity.TipoEvento,
                RecibirEnSistema = entity.RecibirEnSistema,
                RecibirPorCorreo = entity.RecibirPorCorreo
            };

            return new ResponseDto<PreferenciasNotificacionesResponseDto>(201, dto, "Preferencia registrada correctamente.");
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