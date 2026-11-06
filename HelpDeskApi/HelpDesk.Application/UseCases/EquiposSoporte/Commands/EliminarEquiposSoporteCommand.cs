using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;

namespace HelpDesk.Application.UseCases.EquiposSoporte.Commands;

public class EliminarEquiposSoporteCommand : IRequest<ResponseDto<OperacionDto>>
{
    public Guid IdEquipo { get; set; }

    public EliminarEquiposSoporteCommand(Guid idEquipo)
    {
        IdEquipo = idEquipo;
    }
}

internal sealed class EliminarEquiposSoporteCommandHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<EliminarEquiposSoporteCommand, ResponseDto<OperacionDto>>
{
    public async Task<ResponseDto<OperacionDto>> Handle(EliminarEquiposSoporteCommand request, CancellationToken ct)
    {
        try
        {
            if (!access.Admin) access.Permission("equipos_soporte.eliminar");

            var repo = _unitOfWork.Repository<Domain.Models.EquiposSoporte>();
            var entity = await repo.GetByIdAsync([request.IdEquipo]);
            if (entity == null)
            {
                throw new BusinessException(404, "Equipo de soporte no encontrado.");
            }

            entity.Activo = false;
            repo.Update(entity);
            await _unitOfWork.Complete();

            return new ResponseDto<OperacionDto>(200, new OperacionDto(true), "Equipo de soporte desactivado correctamente.");
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<OperacionDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<OperacionDto>(500, default, "No se pudo completar la operacion.");
        }
    }
}