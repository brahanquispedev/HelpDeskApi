using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.IntegrantesEquipo;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.UseCases.IntegrantesEquipo.Queries;

public class ListarIntegrantesEquipoQuery : IRequest<ResponseDto<IEnumerable<IntegrantesEquipoResponseDto>>>
{
    public Guid? IdEquipo { get; set; }
    public bool? SoloActivos { get; set; }
}

internal sealed class ListarIntegrantesEquipoQueryHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<ListarIntegrantesEquipoQuery, ResponseDto<IEnumerable<IntegrantesEquipoResponseDto>>>
{
    public async Task<ResponseDto<IEnumerable<IntegrantesEquipoResponseDto>>> Handle(ListarIntegrantesEquipoQuery request, CancellationToken ct)
    {
        try
        {
            if (!access.Admin) access.Permission("integrantes_equipo.listar");

            var query = _unitOfWork.Repository<Domain.Models.IntegrantesEquipo>().Query();

            if (request.IdEquipo.HasValue)
            {
                query = query.Where(i => i.IdEquipo == request.IdEquipo.Value);
            }

            if (request.SoloActivos.HasValue && request.SoloActivos.Value)
            {
                query = query.Where(i => i.Activo);
            }

            var items = await query
                .Select(i => new IntegrantesEquipoResponseDto
                {
                    IdEquipo = i.IdEquipo,
                    IdUsuario = i.IdUsuario,
                    EsCoordinador = i.EsCoordinador,
                    Disponible = i.Disponible,
                    Activo = i.Activo
                })
                .ToListAsync(ct);

            return new ResponseDto<IEnumerable<IntegrantesEquipoResponseDto>>(200, items, "Integrantes de equipo listados correctamente.");
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<IEnumerable<IntegrantesEquipoResponseDto>>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<IEnumerable<IntegrantesEquipoResponseDto>>(500, default, "No se pudo completar la operacion.");
        }
    }
}