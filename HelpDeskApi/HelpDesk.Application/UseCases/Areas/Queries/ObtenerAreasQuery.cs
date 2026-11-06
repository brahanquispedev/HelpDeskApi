using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Areas;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;

namespace HelpDesk.Application.UseCases.Areas.Queries;

public class ObtenerAreasQuery : IRequest<ResponseDto<AreasResponseDto>>
{
    public Guid IdArea { get; set; }

    public ObtenerAreasQuery(Guid idArea)
    {
        IdArea = idArea;
    }
}

internal sealed class ObtenerAreasQueryHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<ObtenerAreasQuery, ResponseDto<AreasResponseDto>>
{
    public async Task<ResponseDto<AreasResponseDto>> Handle(ObtenerAreasQuery request, CancellationToken ct)
    {
        try
        {
            if (!access.Admin) access.Permission("areas.obtener");

            var area = await _unitOfWork.Repository<Domain.Models.Areas>().GetByIdAsync([request.IdArea]);
            if (area == null)
            {
                throw new BusinessException(404, "Area no encontrada.");
            }

            var dto = new AreasResponseDto
            {
                IdArea = area.IdArea,
                IdSede = area.IdSede,
                Nombre = area.Nombre,
                Activo = area.Activo
            };

            return new ResponseDto<AreasResponseDto>(200, dto, "Area obtenida correctamente.");
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<AreasResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<AreasResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }
}