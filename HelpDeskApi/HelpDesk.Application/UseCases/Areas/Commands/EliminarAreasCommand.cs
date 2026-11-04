using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;

namespace HelpDesk.Application.UseCases.Areas.Commands;

public class EliminarAreasCommand : IRequest<ResponseDto<OperacionDto>>
{
    public Guid IdArea { get; set; }

    public EliminarAreasCommand(Guid idArea)
    {
        IdArea = idArea;
    }
}

internal sealed class EliminarAreasCommandHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<EliminarAreasCommand, ResponseDto<OperacionDto>>
{
    public async Task<ResponseDto<OperacionDto>> Handle(EliminarAreasCommand request, CancellationToken ct)
    {
        try
        {
            if (!access.Admin) access.Permission("areas.eliminar");

            var repo = _unitOfWork.Repository<Domain.Models.Areas>();
            var entity = await repo.GetByIdAsync([request.IdArea]);
            if (entity == null)
            {
                throw new BusinessException(404, "Area no encontrada.");
            }

            // Desactivación lógica
            entity.Activo = false;
            repo.Update(entity);
            await _unitOfWork.Complete();

            return new ResponseDto<OperacionDto>(200, new OperacionDto(true), "Area desactivada correctamente.");
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