using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Application.Services.Security;
using HelpDesk.Domain.Dtos.TareasCambios;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
namespace HelpDesk.Application.UseCases.TareasCambios.Commands;

public class CrearTareasCambiosCommand : IRequest<ResponseDto<TareasCambiosResponseDto>>
{
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

internal sealed class CrearTareasCambiosCommandHandler(IUnitOfWork _unitOfWork, IAccessRules access) : IRequestHandler<CrearTareasCambiosCommand, ResponseDto<TareasCambiosResponseDto>>
{
    public async Task<ResponseDto<TareasCambiosResponseDto>> Handle(CrearTareasCambiosCommand request, CancellationToken ct)
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
            var entity = new global::HelpDesk.Domain.Models.TareasCambios()
            {
                IdTareaCambio = Guid.NewGuid(),
                IdCambio = request.IdCambio,
                IdResponsable = request.IdResponsable,
                Orden = request.Orden,
                Titulo = request.Titulo,
                Descripcion = request.Descripcion,
                Estado = request.Estado,
                FechaCreacion = DateTime.UtcNow
            };
            access.EnsureRead(await Required<global::HelpDesk.Domain.Models.Cambios>(entity.IdCambio));
            entity.FechaFinalizacion = entity.Estado == "completada" ? DateTime.UtcNow : null;
            await _unitOfWork.Repository<global::HelpDesk.Domain.Models.TareasCambios>().AddAsync(entity);
            await _unitOfWork.Complete();
            return new ResponseDto<TareasCambiosResponseDto>(201, new TareasCambiosResponseDto
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
            }, "Registro creado.");
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
