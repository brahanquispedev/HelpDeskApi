using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Application.Services.Security;
using HelpDesk.Domain.Dtos.TicketsActivos;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
namespace HelpDesk.Application.UseCases.TicketsActivos.Commands;

public class CrearTicketsActivosCommand : IRequest<ResponseDto<TicketsActivosResponseDto>>
{
    public Guid IdTicket { get; set; }
    public Guid IdActivo { get; set; }
}

internal sealed class CrearTicketsActivosCommandHandler(IUnitOfWork _unitOfWork, IAccessRules access) : IRequestHandler<CrearTicketsActivosCommand, ResponseDto<TicketsActivosResponseDto>>
{
    public async Task<ResponseDto<TicketsActivosResponseDto>> Handle(CrearTicketsActivosCommand request, CancellationToken ct)
    {
        try
        {

            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            if (request.IdTicket == Guid.Empty)
            {
                throw new ValidationException("IdTicket debe ser un identificador valido.");
            }
            if (request.IdActivo == Guid.Empty)
            {
                throw new ValidationException("IdActivo debe ser un identificador valido.");
            }
            if (!(access.Admin))
            {
                throw new BusinessException(403, "Esta operacion requiere administracion.");
            }
            var entity = new Dictionary<string, object>() { ["IdTicket"] = request.IdTicket, ["IdActivo"] = request.IdActivo };

            await _unitOfWork.Repository<Dictionary<string, object>>("TicketsActivos").AddAsync(entity);
            await _unitOfWork.Complete();
            return new ResponseDto<TicketsActivosResponseDto>(201, new TicketsActivosResponseDto()
            {
                IdTicket = (Guid)entity["IdTicket"],
                IdActivo = (Guid)entity["IdActivo"]
            }, "Registro creado.");
        }
        catch (ValidationException ex)
        {
            return new ResponseDto<TicketsActivosResponseDto>(400, default, ex.Message);
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<TicketsActivosResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<TicketsActivosResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }

}