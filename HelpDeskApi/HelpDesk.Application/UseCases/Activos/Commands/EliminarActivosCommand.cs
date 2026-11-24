using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Application.Common;
using HelpDesk.Application.Services.Security;
using HelpDesk.Domain.Dtos.Activos;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
namespace HelpDesk.Application.UseCases.Activos.Commands;

public class EliminarActivosCommand : IRequest<ResponseDto<OperacionDto>>
{
    [JsonIgnore]
    public object[] Keys { get; set; } = [];
}

internal sealed class EliminarActivosCommandHandler(IUnitOfWork _unitOfWork, IAccessRules access) : IRequestHandler<EliminarActivosCommand, ResponseDto<OperacionDto>>
{
    public async Task<ResponseDto<OperacionDto>> Handle(EliminarActivosCommand request, CancellationToken ct)
    {
        try
        {
            Validation.Require(access.Admin, "Esta operacion requiere administracion.", 403);
            var entity = await _unitOfWork.Repository<global::HelpDesk.Domain.Models.Activos>().GetByIdAsync(request.Keys)
                         ?? throw new BusinessException(404, "Registro no encontrado.");
            access.EnsureRead(entity);

            var asignado = await _unitOfWork.Repository<global::HelpDesk.Domain.Models.AsignacionesActivos>().Query()
                .AnyAsync(x => x.IdActivo == entity.IdActivo && x.FechaDevolucion == null);
            Validation.Require(!asignado, "Devuelva el activo antes de darlo de baja.", 409);
            Validation.Require(entity.Estado != "baja", "El activo ya esta dado de baja.", 409);
            entity.Estado = "baja";
            await _unitOfWork.Complete();
            return new ResponseDto<OperacionDto>(200, new OperacionDto(true), "Activo dado de baja.");
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