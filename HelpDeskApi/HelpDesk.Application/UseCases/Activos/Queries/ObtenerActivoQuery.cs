using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Activos;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using ActivoEntity = HelpDesk.Domain.Models.Activos;
using HelpDesk.Domain.Ports;
using MediatR;

namespace HelpDesk.Application.UseCases.Activos.Queries;

public class ObtenerActivoQuery : IRequest<ResponseDto<ActivosResponseDto>>
{
    public Guid IdActivo { get; set; }
}

internal sealed class ObtenerActivoQueryHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<ObtenerActivoQuery, ResponseDto<ActivosResponseDto>>
{
    public async Task<ResponseDto<ActivosResponseDto>> Handle(ObtenerActivoQuery request, CancellationToken ct)
    {
        try
        {
            var entity = await _unitOfWork.Repository<ActivoEntity>().GetByIdAsync([request.IdActivo])
                ?? throw new BusinessException(404, "Activo no encontrado.");

            access.EnsureRead(entity);

            return new ResponseDto<ActivosResponseDto>(200, new ActivosResponseDto
            {
                IdActivo = entity.IdActivo,
                IdSede = entity.IdSede,
                IdArea = entity.IdArea,
                CodigoPatrimonial = entity.CodigoPatrimonial,
                Nombre = entity.Nombre,
                IdTipoActivo = entity.IdTipoActivo,
                NumeroSerie = entity.NumeroSerie,
                Marca = entity.Marca,
                Modelo = entity.Modelo,
                Estado = entity.Estado,
                FechaRegistro = entity.FechaRegistro
            });
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<ActivosResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<ActivosResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }
}
