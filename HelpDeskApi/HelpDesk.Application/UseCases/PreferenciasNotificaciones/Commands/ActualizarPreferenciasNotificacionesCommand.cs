using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HelpDesk.Application.Common;
using HelpDesk.Application.Services.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.PreferenciasNotificaciones;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;

namespace HelpDesk.Application.UseCases.PreferenciasNotificaciones.Commands;

public class ActualizarPreferenciasNotificacionesCommand : IRequest<ResponseDto<PreferenciasNotificacionesResponseDto>>
{
    [JsonIgnore]
    public Guid IdUsuario { get; set; }

    // Estas claves se reciben en la URL y el controlador las asigna antes del handler.
    [JsonIgnore]
    [StringLength(50)]
    public string TipoEvento { get; set; } = "";

    public bool RecibirEnSistema { get; set; }
    public bool RecibirPorCorreo { get; set; }
}

internal sealed class ActualizarPreferenciasNotificacionesCommandHandler(IUnitOfWork _unitOfWork, IAccessRules access, ICurrentUser actor)
    : IRequestHandler<ActualizarPreferenciasNotificacionesCommand, ResponseDto<PreferenciasNotificacionesResponseDto>>
{
    public async Task<ResponseDto<PreferenciasNotificacionesResponseDto>> Handle(ActualizarPreferenciasNotificacionesCommand request, CancellationToken ct)
    {
        try
        {
            Validation.Check(request);
            Validation.Require(!string.IsNullOrWhiteSpace(request.TipoEvento), "Indique el tipo de evento.");
            if (!access.Admin && request.IdUsuario != actor.Id)
            {
                access.Permission("usuarios.administrar");
            }

            var repo = _unitOfWork.Repository<Domain.Models.PreferenciasNotificaciones>();
            var entity = await repo.GetByIdAsync([request.IdUsuario, request.TipoEvento.Trim()]);
            if (entity == null)
            {
                throw new BusinessException(404, "Preferencia de notificacion no encontrada.");
            }

            entity.RecibirEnSistema = request.RecibirEnSistema;
            entity.RecibirPorCorreo = request.RecibirPorCorreo;

            repo.Update(entity);
            await _unitOfWork.Complete();

            var dto = new PreferenciasNotificacionesResponseDto
            {
                IdUsuario = entity.IdUsuario,
                TipoEvento = entity.TipoEvento,
                RecibirEnSistema = entity.RecibirEnSistema,
                RecibirPorCorreo = entity.RecibirPorCorreo
            };

            return new ResponseDto<PreferenciasNotificacionesResponseDto>(200, dto, "Preferencia actualizada correctamente.");
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
