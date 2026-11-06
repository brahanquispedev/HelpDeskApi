using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.EquiposSoporte;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;

namespace HelpDesk.Application.UseCases.EquiposSoporte.Queries;

public class ObtenerEquiposSoporteQuery : IRequest<ResponseDto<EquiposSoporteResponseDto>>
{
    public Guid IdEquipo { get; set; }

    public ObtenerEquiposSoporteQuery(Guid idEquipo)
    {
        IdEquipo = idEquipo;
    }
}

internal sealed class ObtenerEquiposSoporteQueryHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<ObtenerEquiposSoporteQuery, ResponseDto<EquiposSoporteResponseDto>>
{
    public async Task<ResponseDto<EquiposSoporteResponseDto>> Handle(ObtenerEquiposSoporteQuery request, CancellationToken ct)
    {
        try
        {
            if (!access.Admin) access.Permission("equipos_soporte.obtener");

            var equipo = await _unitOfWork.Repository<Domain.Models.EquiposSoporte>().GetByIdAsync([request.IdEquipo]);
            if (equipo == null)
            {
                throw new BusinessException(404, "Equipo de soporte no encontrado.");
            }

            var dto = new EquiposSoporteResponseDto
            {
                IdEquipo = equipo.IdEquipo,
                Nombre = equipo.Nombre,
                Descripcion = equipo.Descripcion,
                Activo = equipo.Activo
            };

            return new ResponseDto<EquiposSoporteResponseDto>(200, dto, "Equipo de soporte obtenido correctamente.");
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<EquiposSoporteResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<EquiposSoporteResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }
}