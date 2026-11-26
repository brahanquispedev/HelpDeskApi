using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Application.Interfaces;
using HelpDesk.Domain.Dtos.Comentarios;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
namespace HelpDesk.Application.UseCases.Comentarios.Commands;

public class ActualizarComentariosCommand : IRequest<ResponseDto<ComentariosResponseDto>>
{
    [JsonIgnore]
    public object[] Keys { get; set; } = [];

    public Guid IdTicket { get; set; }
    [Required]
    [StringLength(10000)]
    public string Mensaje { get; set; } = "";
    public bool EsInterno { get; set; }
}

internal sealed class ActualizarComentariosCommandHandler(IUnitOfWork _unitOfWork, IAccessRules access, ICurrentUser actor) : IRequestHandler<ActualizarComentariosCommand, ResponseDto<ComentariosResponseDto>>
{
    public async Task<ResponseDto<ComentariosResponseDto>> Handle(ActualizarComentariosCommand request, CancellationToken ct)
    {
        try
        {

            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            if (request.IdTicket == Guid.Empty)
            {
                throw new ValidationException("IdTicket debe ser un identificador valido.");
            }
            if (!(access.Admin))
            {
                throw new BusinessException(403, "Esta operacion requiere administracion.");
            }
            var entity = await _unitOfWork.Repository<global::HelpDesk.Domain.Models.Comentarios>().GetByIdAsync(request.Keys)
            ?? throw new BusinessException(404, "Registro no encontrado.");
            access.EnsureRead(entity);

            entity.IdTicket = request.IdTicket;
            entity.Mensaje = request.Mensaje;
            entity.EsInterno = request.EsInterno;
            var ticket = await Required<global::HelpDesk.Domain.Models.Tickets>(entity.IdTicket);
            access.EnsureRead(ticket);
            if (entity.EsInterno) access.Permission("comentarios.internos");
            if (!(ticket.Estado is not ("cerrado" or "cancelado")))
            {
                throw new BusinessException(409, "No se agregan mensajes a un ticket finalizado.");
            }
            if (!(access.Admin || entity.IdAutor == actor.Id))
            {
                throw new BusinessException(403, "No puede editar mensajes ajenos.");
            }

            await _unitOfWork.Complete();
            return new ResponseDto<ComentariosResponseDto>(200, new ComentariosResponseDto
            {
                IdComentario = entity.IdComentario,
                IdTicket = entity.IdTicket,
                IdAutor = entity.IdAutor,
                Mensaje = entity.Mensaje,
                EsInterno = entity.EsInterno,
                Fecha = entity.Fecha
            }, "Registro actualizado.");
        }
        catch (ValidationException ex)
        {
            return new ResponseDto<ComentariosResponseDto>(400, default, ex.Message);
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<ComentariosResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<ComentariosResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }

    private async Task<T> Required<T>(Guid id) where T : class =>
        await _unitOfWork.Repository<T>().GetByIdAsync([id]) ?? throw new BusinessException(404, "Registro relacionado no encontrado.");

}