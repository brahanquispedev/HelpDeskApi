using HelpDesk.Application.Interfaces;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.Sedes;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.UseCases.Sedes.Queries;

public class ObtenerSedesQuery : IRequest<ResponseDto<SedesResponseDto>>
{
    public Guid IdSede { get; set; }

    public ObtenerSedesQuery(Guid idSede)
    {
        IdSede = idSede;
    }
}

internal sealed class ObtenerSedesQueryHandler(IUnitOfWork _unitOfWork, IAccessRules access)
    : IRequestHandler<ObtenerSedesQuery, ResponseDto<SedesResponseDto>>
{
    public async Task<ResponseDto<SedesResponseDto>> Handle(ObtenerSedesQuery request, CancellationToken ct)
    {
        try
        {


            var sede = await _unitOfWork.Repository<Domain.Models.Sedes>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdSede == request.IdSede);
            if (sede == null)
            {
                throw new BusinessException(404, "Sede no encontrada.");
            }

            access.EnsureRead(sede);

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