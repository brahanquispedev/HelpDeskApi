using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.Sedes;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;

namespace HelpDesk.Application.UseCases.Sedes.Queries;

public class ObtenerSedesQuery : IRequest<ResponseDto<SedesResponseDto>>
{
    public Guid IdSede { get; set; }

    public ObtenerSedesQuery(Guid idSede)
    {
        IdSede = idSede;
    }
}

internal sealed class ObtenerSedesQueryHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<ObtenerSedesQuery, ResponseDto<SedesResponseDto>>
{
    public async Task<ResponseDto<SedesResponseDto>> Handle(ObtenerSedesQuery request, CancellationToken ct)
    {
        try
        {
            if (!access.Admin) access.Permission("sedes.obtener");

            var sede = await _unitOfWork.Repository<Domain.Models.Sedes>().GetByIdAsync([request.IdSede]);
            if (sede == null)
            {
                throw new BusinessException(404, "Sede no encontrada.");
            }

            var dto = new SedesResponseDto
            {
                IdSede = sede.IdSede,
                Nombre = sede.Nombre,
                Direccion = sede.Direccion,
                Activo = sede.Activo
            };

            return new ResponseDto<SedesResponseDto>(200, dto, "Sede obtenida correctamente.");
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<SedesResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<SedesResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }
}