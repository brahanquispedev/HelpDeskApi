using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;

namespace HelpDesk.Application.UseCases.Sedes.Commands;

public class EliminarSedesCommand : IRequest<ResponseDto<OperacionDto>>
{
    public Guid IdSede { get; set; }

    public EliminarSedesCommand(Guid idSede)
    {
        IdSede = idSede;
    }
}

internal sealed class EliminarSedesCommandHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<EliminarSedesCommand, ResponseDto<OperacionDto>>
{
    public async Task<ResponseDto<OperacionDto>> Handle(EliminarSedesCommand request, CancellationToken ct)
    {
        try
        {
            if (!access.Admin) access.Permission("sedes.eliminar");

            var repo = _unitOfWork.Repository<Domain.Models.Sedes>();
            var entity = await repo.GetByIdAsync([request.IdSede]);
            if (entity == null)
            {
                throw new BusinessException(404, "Sede no encontrada.");
            }

            // Desactivación lógica para preservar integridad histórica
            entity.Activo = false;
            repo.Update(entity);
            await _unitOfWork.Complete();

            return new ResponseDto<OperacionDto>(200, new OperacionDto(true), "Sede desactivada correctamente.");
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