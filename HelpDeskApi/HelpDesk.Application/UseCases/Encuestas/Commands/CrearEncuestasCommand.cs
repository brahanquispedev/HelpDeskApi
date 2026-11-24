using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Application.Services.Security;
using HelpDesk.Domain.Dtos.Encuestas;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
namespace HelpDesk.Application.UseCases.Encuestas.Commands;

public class CrearEncuestasCommand : IRequest<ResponseDto<EncuestasResponseDto>>
{
    public Guid IdTicket { get; set; }
    public short Calificacion { get; set; }
    public string? Comentario { get; set; }
}

internal sealed class CrearEncuestasCommandHandler(IUnitOfWork _unitOfWork, IAccessRules access, ICurrentUser actor) : IRequestHandler<CrearEncuestasCommand, ResponseDto<EncuestasResponseDto>>
{
    public async Task<ResponseDto<EncuestasResponseDto>> Handle(CrearEncuestasCommand request, CancellationToken ct)
    {
        try
        {

            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            if (request.IdTicket == Guid.Empty)
            {
                throw new ValidationException("IdTicket debe ser un identificador valido.");
            }
            if (!access.Admin) access.Permission("encuestas.responder_propias");
            var entity = new global::HelpDesk.Domain.Models.Encuestas()
            {
                IdEncuesta = Guid.NewGuid(),
                IdTicket = request.IdTicket,
                Calificacion = request.Calificacion,
                Comentario = request.Comentario,
                FechaRespuesta = DateTime.UtcNow
            };
            var evaluated = await Required<global::HelpDesk.Domain.Models.Tickets>(entity.IdTicket);
            if (!(evaluated.IdSolicitante == actor.Id))
            {
                throw new BusinessException(403, "Solo el solicitante puede responder.");
            }
            if (!(evaluated.Estado == "cerrado"))
            {
                throw new BusinessException(409, "El ticket debe estar cerrado.");
            }
            entity.IdUsuario = actor.Id;

            await _unitOfWork.Repository<global::HelpDesk.Domain.Models.Encuestas>().AddAsync(entity);
            await _unitOfWork.Complete();
            return new ResponseDto<EncuestasResponseDto>(201, new EncuestasResponseDto
            {
                IdEncuesta = entity.IdEncuesta,
                IdTicket = entity.IdTicket,
                IdUsuario = entity.IdUsuario,
                Calificacion = entity.Calificacion,
                Comentario = entity.Comentario,
                FechaRespuesta = entity.FechaRespuesta
            }, "Registro creado.");
        }
        catch (ValidationException ex)
        {
            return new ResponseDto<EncuestasResponseDto>(400, default, ex.Message);
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<EncuestasResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<EncuestasResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }

    private async Task<T> Required<T>(Guid id) where T : class =>
        await _unitOfWork.Repository<T>().GetByIdAsync([id]) ?? throw new BusinessException(404, "Registro relacionado no encontrado.");

}