using HelpDesk.Application.Services.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.EquiposSoporte;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.UseCases.EquiposSoporte.Queries;

public class ListarEquiposSoporteQuery : IRequest<ResponseDto<IEnumerable<EquiposSoporteResponseDto>>>
{
    public bool? SoloActivos { get; set; }
}

internal sealed class ListarEquiposSoporteQueryHandler(IUnitOfWork _unitOfWork, IAccessRules access)
    : IRequestHandler<ListarEquiposSoporteQuery, ResponseDto<IEnumerable<EquiposSoporteResponseDto>>>
{
    public async Task<ResponseDto<IEnumerable<EquiposSoporteResponseDto>>> Handle(ListarEquiposSoporteQuery request, CancellationToken ct)
    {
        try
        {


            var query = _unitOfWork.Repository<Domain.Models.EquiposSoporte>().Query();
            query = access.Scope(query);

            if (request.SoloActivos.HasValue && request.SoloActivos.Value)
            {
                query = query.Where(e => e.Activo);
            }

            var items = await query
                .OrderBy(e => e.Nombre)
                .Select(e => new EquiposSoporteResponseDto
                {
                    IdEquipo = e.IdEquipo,
                    Nombre = e.Nombre,
                    Descripcion = e.Descripcion,
                    Activo = e.Activo
                })
                .ToListAsync(ct);

            return new ResponseDto<IEnumerable<EquiposSoporteResponseDto>>(200, items, "Equipos de soporte listados correctamente.");
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<IEnumerable<EquiposSoporteResponseDto>>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<IEnumerable<EquiposSoporteResponseDto>>(500, default, "No se pudo completar la operacion.");
        }
    }
}