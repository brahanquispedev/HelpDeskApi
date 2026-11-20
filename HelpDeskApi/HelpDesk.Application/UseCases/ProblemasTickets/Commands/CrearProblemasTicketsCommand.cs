using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Application.Services.Security;
using HelpDesk.Domain.Dtos.ProblemasTickets;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
namespace HelpDesk.Application.UseCases.ProblemasTickets.Commands;

public class CrearProblemasTicketsCommand : IRequest<ResponseDto<ProblemasTicketsResponseDto>>
{
    public Guid IdProblema { get; set; }
    public Guid IdTicket { get; set; }
}

internal sealed class CrearProblemasTicketsCommandHandler(IUnitOfWork _unitOfWork, AccessRules access) : IRequestHandler<CrearProblemasTicketsCommand, ResponseDto<ProblemasTicketsResponseDto>>
{
    public async Task<ResponseDto<ProblemasTicketsResponseDto>> Handle(CrearProblemasTicketsCommand request, CancellationToken ct)
    {
        try
        {

            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            if (request.IdProblema == Guid.Empty)
            {
                throw new ValidationException("IdProblema debe ser un identificador valido.");
            }
            if (request.IdTicket == Guid.Empty)
            {
                throw new ValidationException("IdTicket debe ser un identificador valido.");
            }
            if (!(access.Admin))
            {
                throw new BusinessException(403, "Esta operacion requiere administracion.");
            }
            var entity = new Dictionary<string, object>() { ["IdProblema"] = request.IdProblema, ["IdTicket"] = request.IdTicket };

            await _unitOfWork.Repository<Dictionary<string, object>>("ProblemasTickets").AddAsync(entity);
            await _unitOfWork.Complete();
            return new ResponseDto<ProblemasTicketsResponseDto>(201, new ProblemasTicketsResponseDto()
            {
                IdProblema = (Guid)entity["IdProblema"],
                IdTicket = (Guid)entity["IdTicket"]
            }, "Registro creado.");
        }
        catch (ValidationException ex)
        {
            return new ResponseDto<ProblemasTicketsResponseDto>(400, default, ex.Message);
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<ProblemasTicketsResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<ProblemasTicketsResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }

}
