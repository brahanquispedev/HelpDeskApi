using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Application.Services.Security;
using HelpDesk.Domain.Dtos.Adjuntos;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
namespace HelpDesk.Application.UseCases.Adjuntos.Commands;

public class CrearAdjuntosCommand : IRequest<ResponseDto<AdjuntosResponseDto>>
{
    public Guid IdTicket { get; set; }
    public Guid? IdComentario { get; set; }
    [Required]
    [StringLength(255)]
    public string NombreArchivo { get; set; } = "";
    [Required]
    [StringLength(150)]
    public string TipoArchivo { get; set; } = "";
    [Range(1, long.MaxValue)]
    public long TamanoBytes { get; set; }
    public bool EsInterno { get; set; }
}

internal sealed class CrearAdjuntosCommandHandler(IUnitOfWork _unitOfWork, IAccessRules access, ICurrentUser actor) : IRequestHandler<CrearAdjuntosCommand, ResponseDto<AdjuntosResponseDto>>
{
    public async Task<ResponseDto<AdjuntosResponseDto>> Handle(CrearAdjuntosCommand request, CancellationToken ct)
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
            var entity = new global::HelpDesk.Domain.Models.Adjuntos()
            {
                IdAdjunto = Guid.NewGuid(),
                IdTicket = request.IdTicket,
                IdComentario = request.IdComentario,
                NombreArchivo = request.NombreArchivo,
                TipoArchivo = request.TipoArchivo,
                TamanoBytes = request.TamanoBytes,
                EsInterno = request.EsInterno,
                Fecha = DateTime.UtcNow
            };
            var attachedTicket = await Required<global::HelpDesk.Domain.Models.Tickets>(entity.IdTicket);
            // Primera version: solo metadatos. Esta referencia no representa un archivo subido.
            entity.UbicacionArchivo = $"metadatos/{entity.IdAdjunto:N}";
            access.EnsureRead(attachedTicket);
            if (entity.EsInterno) access.Permission("comentarios.internos");
            entity.IdUsuario = actor.Id;
            if (entity.IdComentario is Guid commentId)
            {
                var parent = await Required<global::HelpDesk.Domain.Models.Comentarios>(commentId);
                if (!(parent.IdTicket == entity.IdTicket))
                {
                    throw new BusinessException(400, "El comentario pertenece a otro ticket.");
                }
                if (parent.EsInterno) entity.EsInterno = true;
            }

            await _unitOfWork.Repository<global::HelpDesk.Domain.Models.Adjuntos>().AddAsync(entity);
            await _unitOfWork.Complete();
            return new ResponseDto<AdjuntosResponseDto>(201, new AdjuntosResponseDto
            {
                IdAdjunto = entity.IdAdjunto,
                IdTicket = entity.IdTicket,
                IdComentario = entity.IdComentario,
                IdUsuario = entity.IdUsuario,
                NombreArchivo = entity.NombreArchivo,
                TipoArchivo = entity.TipoArchivo,
                TamanoBytes = entity.TamanoBytes,
                EsInterno = entity.EsInterno,
                Fecha = entity.Fecha
            }, "Registro creado.");
        }
        catch (ValidationException ex)
        {
            return new ResponseDto<AdjuntosResponseDto>(400, default, ex.Message);
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<AdjuntosResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<AdjuntosResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }

    private async Task<T> Required<T>(Guid id) where T : class =>
        await _unitOfWork.Repository<T>().GetByIdAsync([id]) ?? throw new BusinessException(404, "Registro relacionado no encontrado.");

}
