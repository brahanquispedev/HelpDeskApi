using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.Sedes;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.UseCases.Sedes.Queries;

public class ListarSedesQuery : IRequest<ResponseDto<IEnumerable<SedesResponseDto>>>
{
    public bool? SoloActivos { get; set; }
}

internal sealed class ListarSedesQueryHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<ListarSedesQuery, ResponseDto<IEnumerable<SedesResponseDto>>>
{
    public async Task<ResponseDto<IEnumerable<SedesResponseDto>>> Handle(ListarSedesQuery request, CancellationToken ct)
    {
        try
        {
            if (!access.Admin) access.Permission("sedes.listar");

            var query = _unitOfWork.Repository<Domain.Models.Sedes>().Query();

            if (request.SoloActivos.HasValue && request.SoloActivos.Value)
            {
                query = query.Where(s => s.Activo);
            }

            var items = await query
                .OrderBy(s => s.Nombre)
                .Select(s => new SedesResponseDto
                {
                    IdSede = s.IdSede,
                    Nombre = s.Nombre,
                    Direccion = s.Direccion,
                    Activo = s.Activo
                })
                .ToListAsync(ct);

            return new ResponseDto<IEnumerable<SedesResponseDto>>(200, items, "Sedes listadas correctamente.");
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<IEnumerable<SedesResponseDto>>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<IEnumerable<SedesResponseDto>>(500, default, "No se pudo completar la operacion.");
        }
    }
}