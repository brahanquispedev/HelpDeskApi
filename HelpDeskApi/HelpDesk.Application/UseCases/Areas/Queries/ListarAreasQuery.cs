using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Areas;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.UseCases.Areas.Queries;

public class ListarAreasQuery : IRequest<ResponseDto<IEnumerable<AreasResponseDto>>>
{
    public Guid? IdSede { get; set; }
    public bool? SoloActivos { get; set; }
}

internal sealed class ListarAreasQueryHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<ListarAreasQuery, ResponseDto<IEnumerable<AreasResponseDto>>>
{
    public async Task<ResponseDto<IEnumerable<AreasResponseDto>>> Handle(ListarAreasQuery request, CancellationToken ct)
    {
        try
        {
            if (!access.Admin) access.Permission("areas.listar");

            var query = _unitOfWork.Repository<Domain.Models.Areas>().Query();

            if (request.IdSede.HasValue)
            {
                query = query.Where(a => a.IdSede == request.IdSede.Value);
            }

            if (request.SoloActivos.HasValue && request.SoloActivos.Value)
            {
                query = query.Where(a => a.Activo);
            }

            var items = await query
                .OrderBy(a => a.Nombre)
                .Select(a => new AreasResponseDto
                {
                    IdArea = a.IdArea,
                    IdSede = a.IdSede,
                    Nombre = a.Nombre,
                    Activo = a.Activo
                })
                .ToListAsync(ct);

            return new ResponseDto<IEnumerable<AreasResponseDto>>(200, items, "Areas listadas correctamente.");
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<IEnumerable<AreasResponseDto>>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<IEnumerable<AreasResponseDto>>(500, default, "No se pudo completar la operacion.");
        }
    }
}