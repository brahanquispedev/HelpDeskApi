using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Dtos.Common;
using Microsoft.EntityFrameworkCore;
using HelpDesk.Application.Services.Security;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
namespace HelpDesk.Application.UseCases.Aprobaciones.Commands;

public class RechazarSolicitudCommand : IRequest<ResponseDto<AccionResultadoDto>>
{
    [JsonIgnore]
    public Guid Id { get; set; }

    [StringLength(4000)]
    public string Motivo { get; set; } = string.Empty;
}

internal sealed class RechazarSolicitudCommandHandler(IUnitOfWork _unitOfWork, ICurrentUser actor, IAccessRules access)
    : IRequestHandler<RechazarSolicitudCommand, ResponseDto<AccionResultadoDto>>
{
    public async Task<ResponseDto<AccionResultadoDto>> Handle(RechazarSolicitudCommand r, CancellationToken ct)
    {
        try
        {
            Validator.ValidateObject(r, new ValidationContext(r), validateAllProperties: true);
            if (r.Id == Guid.Empty)
            {
                throw new ValidationException("Id debe ser un identificador valido.");
            }
            var result = "completado";
            access.Permission("aprobaciones.decidir");
            var a = await Required<global::HelpDesk.Domain.Models.Aprobaciones>(r.Id);
            if (!(a.IdAprobador == actor.Id))
            {
                throw new BusinessException(403, "Solo el aprobador asignado puede decidir.");
            }
            if (!(a.Estado == "pendiente"))
            {
                throw new BusinessException(409, "Ya fue decidida.");
            }
            var previous = await (_unitOfWork.Repository<global::HelpDesk.Domain.Models.Aprobaciones>().Query().Where(x => x.IdTicket == a.IdTicket && x.IdCambio == a.IdCambio && x.Orden < a.Orden)).ToListAsync();
            if (!(previous.All(x => x.Estado == "aprobado")))
            {
                throw new BusinessException(409, "Complete los pasos anteriores en orden.");
            }
            if (!(!string.IsNullOrWhiteSpace(r.Motivo)))
            {
                throw new BusinessException(400, "Indique el motivo de la decision.");
            }
            a.Estado = "rechazado";
            a.FechaDecision = DateTime.UtcNow;
            a.ComentarioDecision = r.Motivo;
            result = a.Estado;
            await _unitOfWork.Complete();
            return new ResponseDto<AccionResultadoDto>(200, new AccionResultadoDto(r.Id, result));
        }
        catch (ValidationException ex)
        {
            return new ResponseDto<AccionResultadoDto>(400, default, ex.Message);
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<AccionResultadoDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<AccionResultadoDto>(500, default, "No se pudo completar la operacion.");
        }
    }

    private async Task<T> Required<T>(Guid id) where T : class =>
        await _unitOfWork.Repository<T>().GetByIdAsync([id]) ?? throw new BusinessException(404, "Registro relacionado no encontrado.");
}
