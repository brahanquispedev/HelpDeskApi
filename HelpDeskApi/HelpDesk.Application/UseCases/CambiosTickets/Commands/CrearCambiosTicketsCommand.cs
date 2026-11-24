using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Application.Services.Security;
using HelpDesk.Domain.Dtos.CambiosTickets;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
namespace HelpDesk.Application.UseCases.CambiosTickets.Commands;

public class CrearCambiosTicketsCommand : IRequest<ResponseDto<CambiosTicketsResponseDto>>
{
    public Guid IdCambio { get; set; }
    public Guid IdTicket { get; set; }
}

internal sealed class CrearCambiosTicketsCommandHandler(IUnitOfWork _unitOfWork, IAccessRules access) : IRequestHandler<CrearCambiosTicketsCommand, ResponseDto<CambiosTicketsResponseDto>>
{
    public async Task<ResponseDto<CambiosTicketsResponseDto>> Handle(CrearCambiosTicketsCommand request, CancellationToken ct)
    {
        try
        {

            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            if (request.IdCambio == Guid.Empty)
            {
                throw new ValidationException("IdCambio debe ser un identificador valido.");
            }
            if (request.IdTicket == Guid.Empty)
            {
                throw new ValidationException("IdTicket debe ser un identificador valido.");
            }
            if (!(access.Admin))
            {
                throw new BusinessException(403, "Esta operacion requiere administracion.");
            }
            var entity = new Dictionary<string, object>() { ["IdCambio"] = request.IdCambio, ["IdTicket"] = request.IdTicket };

            await _unitOfWork.Repository<Dictionary<string, object>>("CambiosTickets").AddAsync(entity);
            await _unitOfWork.Complete();
            return new ResponseDto<CambiosTicketsResponseDto>(201, new CambiosTicketsResponseDto()
            {
                IdCambio = (Guid)entity["IdCambio"],
                IdTicket = (Guid)entity["IdTicket"]
            }, "Registro creado.");
        }
        catch (ValidationException ex)
        {
            return new ResponseDto<CambiosTicketsResponseDto>(400, default, ex.Message);
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<CambiosTicketsResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<CambiosTicketsResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }

}
