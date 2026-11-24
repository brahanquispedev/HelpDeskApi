using HelpDesk.Domain.Dtos.Common;
using Microsoft.EntityFrameworkCore;
using HelpDesk.Application.Services.Security;
using HelpDesk.Domain.Dtos.TicketsActivos;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
namespace HelpDesk.Application.UseCases.TicketsActivos.Queries;

public class ListarTicketsActivosQuery : IRequest<ResponseDto<PaginaDto<TicketsActivosResponseDto>>>
{
    public int Pagina { get; set; } = 1;
    public int Tamano { get; set; } = 20;
}

internal sealed class ListarTicketsActivosQueryHandler(IUnitOfWork _unitOfWork, IAccessRules access) : IRequestHandler<ListarTicketsActivosQuery, ResponseDto<PaginaDto<TicketsActivosResponseDto>>>
{
    public async Task<ResponseDto<PaginaDto<TicketsActivosResponseDto>>> Handle(ListarTicketsActivosQuery request, CancellationToken ct)
    {
        try
        {

            if (!(request.Pagina >= 1 && request.Pagina <= 1000000 && request.Tamano is >= 1 and <= 100))
            {
                throw new BusinessException(400, "Pagina debe ser positiva y tamano debe estar entre 1 y 100.");
            }
            var query = access.Scope(_unitOfWork.Repository<Dictionary<string, object>>("TicketsActivos").Query());

            var total = await (query).CountAsync();
            var items = await (query.OrderBy(x => x["IdTicket"]).ThenBy(x => x["IdActivo"]).Skip((request.Pagina - 1) * request.Tamano).Take(request.Tamano)
            .Select(entity => new TicketsActivosResponseDto()
            {
                IdTicket = (Guid)entity["IdTicket"],
                IdActivo = (Guid)entity["IdActivo"]
            })).ToListAsync();
            return new ResponseDto<PaginaDto<TicketsActivosResponseDto>>(200, new PaginaDto<TicketsActivosResponseDto>(items, request.Pagina, request.Tamano, total));
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<PaginaDto<TicketsActivosResponseDto>>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<PaginaDto<TicketsActivosResponseDto>>(500, default, "No se pudo completar la operacion.");
        }
    }

}