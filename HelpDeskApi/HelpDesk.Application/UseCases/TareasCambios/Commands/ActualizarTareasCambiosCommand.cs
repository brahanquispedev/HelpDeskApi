using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Application.Interfaces;
using HelpDesk.Domain.Dtos.TareasCambios;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
namespace HelpDesk.Application.UseCases.TareasCambios.Commands;

public class ActualizarTareasCambiosCommand : IRequest<ResponseDto<TareasCambiosResponseDto>>
{
    [JsonIgnore]
    public object[] Keys { get; set; } = [];

    public Guid IdCambio { get; set; }
    public Guid? IdResponsable { get; set; }
    public short Orden { get; set; } = 1;
    [Required]
    [StringLength(10000)]
    public string Titulo { get; set; } = "";
    public string? Descripcion { get; set; }
    [Required]
    [StringLength(10000)]
    public string Estado { get; set; } = "pendiente";
}

internal sealed class ActualizarTareasCambiosCommandHandler(IUnitOfWork _unitOfWork, IAccessRules access) : IRequestHandler<ActualizarTareasCambiosCommand, ResponseDto<TareasCambiosResponseDto>>
{
    public async Task<ResponseDto<TareasCambiosResponseDto>> Handle(ActualizarTareasCambiosCommand request, CancellationToken ct)
    {
        try
        {

            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            if (request.IdCambio == Guid.Empty)
            {
                throw new ValidationException("IdCambio debe ser un identificador valido.");
            }
            if (!(access.Admin))
            {
                throw new BusinessException(403, "Esta operacion requiere administracion.");
            }
            var entity = await _unitOfWork.Repository<global::HelpDesk.Domain.Models.TareasCambios>().GetByIdAsync(request.Keys)
            ?? throw new BusinessException(404, "Registro no encontrado.");
            access.EnsureRead(entity);

            entity.IdCambio = request.IdCambio;
            entity.IdResponsable = request.IdResponsable;
            entity.Orden = request.Orden;
            entity.Titulo = request.Titulo;
            entity.Descripcion = request.Descripcion;
            entity.Estado = request.Estado;
            access.EnsureRead(await Required<global::HelpDesk.Domain.Models.Cambios>(entity.IdCambio));
            entity.FechaFinalizacion = entity.Estado == "completada" ? DateTime.UtcNow : null;
            await _unitOfWork.Complete();
            return new ResponseDto<TareasCambiosResponseDto>(200, new TareasCambiosResponseDto
            {
                IdTareaCambio = entity.IdTareaCambio,
                IdCambio = entity.IdCambio,
                IdResponsable = entity.IdResponsable,
                Orden = entity.Orden,
                Titulo = entity.Titulo,
                Descripcion = entity.Descripcion,
                Estado = entity.Estado,
                FechaCreacion = entity.FechaCreacion,
                FechaFinalizacion = entity.FechaFinalizacion
            }, "Registro actualizado.");
        }
        catch (ValidationException ex)
        {
            return new ResponseDto<TareasCambiosResponseDto>(400, default, ex.Message);
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<TareasCambiosResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<TareasCambiosResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }

    private async Task<T> Required<T>(Guid id) where T : class =>
        await _unitOfWork.Repository<T>().GetByIdAsync([id]) ?? throw new BusinessException(404, "Registro relacionado no encontrado.");

}
