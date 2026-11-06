using HelpDesk.Application.Common;
using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.IntegrantesEquipo;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;

namespace HelpDesk.Application.UseCases.IntegrantesEquipo.Commands;

public class ActualizarIntegrantesEquipoCommand : IRequest<ResponseDto<IntegrantesEquipoResponseDto>>
{
    public Guid IdEquipo { get; set; }
    public Guid IdUsuario { get; set; }
    public bool EsCoordinador { get; set; }
    public bool Disponible { get; set; }
    public bool Activo { get; set; }
}

internal sealed class ActualizarIntegrantesEquipoCommandHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<ActualizarIntegrantesEquipoCommand, ResponseDto<IntegrantesEquipoResponseDto>>
{
    public async Task<ResponseDto<IntegrantesEquipoResponseDto>> Handle(ActualizarIntegrantesEquipoCommand request, CancellationToken ct)
    {
        try
        {
            Validation.Check(request);
            if (!access.Admin) access.Permission("integrantes_equipo.actualizar");

            var repo = _unitOfWork.Repository<Domain.Models.IntegrantesEquipo>();
            var entity = await repo.GetByIdAsync([request.IdEquipo, request.IdUsuario]);
            if (entity == null)
            {
                throw new BusinessException(404, "Integrante de equipo no encontrado.");
            }

            entity.EsCoordinador = request.EsCoordinador;
            entity.Disponible = request.Disponible;
            entity.Activo = request.Activo;

            repo.Update(entity);
            await _unitOfWork.Complete();

            var dto = new IntegrantesEquipoResponseDto
            {
                IdEquipo = entity.IdEquipo,
                IdUsuario = entity.IdUsuario,
                EsCoordinador = entity.EsCoordinador,
                Disponible = entity.Disponible,
                Activo = entity.Activo
            };

            return new ResponseDto<IntegrantesEquipoResponseDto>(200, dto, "Integrante de equipo actualizado correctamente.");
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