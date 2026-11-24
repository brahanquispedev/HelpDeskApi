using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Application.Services.Security;
using HelpDesk.Domain.Dtos.TicketsArticulos;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
namespace HelpDesk.Application.UseCases.TicketsArticulos.Commands;

public class CrearTicketsArticulosCommand : IRequest<ResponseDto<TicketsArticulosResponseDto>>
{
    public Guid IdTicket { get; set; }
    public Guid IdArticulo { get; set; }
    public Guid IdUsuario { get; set; }
}

internal sealed class CrearTicketsArticulosCommandHandler(IUnitOfWork _unitOfWork, IAccessRules access, ICurrentUser actor) : IRequestHandler<CrearTicketsArticulosCommand, ResponseDto<TicketsArticulosResponseDto>>
{
    public async Task<ResponseDto<TicketsArticulosResponseDto>> Handle(CrearTicketsArticulosCommand request, CancellationToken ct)
    {
        try
        {

            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            if (request.IdTicket == Guid.Empty)
            {
                throw new ValidationException("IdTicket debe ser un identificador valido.");
            }
            if (request.IdArticulo == Guid.Empty)
            {
                throw new ValidationException("IdArticulo debe ser un identificador valido.");
            }
            if (request.IdUsuario == Guid.Empty)
            {
                throw new ValidationException("IdUsuario debe ser un identificador valido.");
            }
            if (!(access.Admin))
            {
                throw new BusinessException(403, "Esta operacion requiere administracion.");
            }
            var entity = new global::HelpDesk.Domain.Models.TicketsArticulos()
            {
                IdTicket = request.IdTicket,
                IdArticulo = request.IdArticulo,
                IdUsuario = request.IdUsuario,
                FechaVinculacion = DateTime.UtcNow
            };
            entity.IdUsuario = actor.Id;
            await _unitOfWork.Repository<global::HelpDesk.Domain.Models.TicketsArticulos>().AddAsync(entity);
            await _unitOfWork.Complete();
            return new ResponseDto<TicketsArticulosResponseDto>(201, new TicketsArticulosResponseDto
            {
                IdTicket = entity.IdTicket,
                IdArticulo = entity.IdArticulo,
                IdUsuario = entity.IdUsuario,
                FechaVinculacion = entity.FechaVinculacion
            }, "Registro creado.");
        }
        catch (ValidationException ex)
        {
            return new ResponseDto<TicketsArticulosResponseDto>(400, default, ex.Message);
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<TicketsArticulosResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<TicketsArticulosResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }

}