using HelpDesk.Application.Interfaces;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.IntegrantesEquipo;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.UseCases.IntegrantesEquipo.Queries;

public class ObtenerIntegrantesEquipoQuery : IRequest<ResponseDto<IntegrantesEquipoResponseDto>>
{
    public Guid IdEquipo { get; set; }
    public Guid IdUsuario { get; set; }

    public ObtenerIntegrantesEquipoQuery(Guid idEquipo, Guid idUsuario)
    {
        IdEquipo = idEquipo;
        IdUsuario = idUsuario;
    }
}

internal sealed class ObtenerIntegrantesEquipoQueryHandler(IUnitOfWork _unitOfWork, IAccessRules access)
    : IRequestHandler<ObtenerIntegrantesEquipoQuery, ResponseDto<IntegrantesEquipoResponseDto>>
{
    public async Task<ResponseDto<IntegrantesEquipoResponseDto>> Handle(ObtenerIntegrantesEquipoQuery request, CancellationToken ct)
    {
        try
        {
            if (!access.Admin) access.Permission("organizacion.administrar");

            var integrante = await _unitOfWork.Repository<Domain.Models.IntegrantesEquipo>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdEquipo == request.IdEquipo && x.IdUsuario == request.IdUsuario);
            if (integrante == null)
            {
                throw new BusinessException(404, "Integrante de equipo no encontrado.");
            }

            var dto = new IntegrantesEquipoResponseDto
            {
                IdEquipo = integrante.IdEquipo,
                IdUsuario = integrante.IdUsuario,
                EsCoordinador = integrante.EsCoordinador,
                Disponible = integrante.Disponible,
                Activo = integrante.Activo
            };

            return new ResponseDto<IntegrantesEquipoResponseDto>(200, dto, "Integrante de equipo obtenido correctamente.");
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<IntegrantesEquipoResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<IntegrantesEquipoResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }
}