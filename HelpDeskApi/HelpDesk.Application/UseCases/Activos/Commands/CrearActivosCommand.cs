using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Application.Services.Security;
using HelpDesk.Domain.Dtos.Activos;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
namespace HelpDesk.Application.UseCases.Activos.Commands;

public class CrearActivosCommand : IRequest<ResponseDto<ActivosResponseDto>>
{
    public Guid IdSede { get; set; }
    public Guid? IdArea { get; set; }
    [Required]
    [StringLength(10000)]
    public string CodigoPatrimonial { get; set; } = "";
    [Required]
    [StringLength(10000)]
    public string Nombre { get; set; } = "";
    public Guid IdTipoActivo { get; set; }
    public string? NumeroSerie { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    [Required]
    [StringLength(10000)]
    public string Estado { get; set; } = "disponible";
}

internal sealed class CrearActivosCommandHandler(IUnitOfWork _unitOfWork, AccessRules access) : IRequestHandler<CrearActivosCommand, ResponseDto<ActivosResponseDto>>
{
    public async Task<ResponseDto<ActivosResponseDto>> Handle(CrearActivosCommand request, CancellationToken ct)
    {
        try
        {

            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            if (request.IdSede == Guid.Empty)
            {
                throw new ValidationException("IdSede debe ser un identificador valido.");
            }
            if (request.IdTipoActivo == Guid.Empty)
            {
                throw new ValidationException("IdTipoActivo debe ser un identificador valido.");
            }
            if (!(access.Admin))
            {
                throw new BusinessException(403, "Esta operacion requiere administracion.");
            }
            var entity = new global::HelpDesk.Domain.Models.Activos()
            {
                IdActivo = Guid.NewGuid(),
                IdSede = request.IdSede,
                IdArea = request.IdArea,
                CodigoPatrimonial = request.CodigoPatrimonial,
                Nombre = request.Nombre,
                IdTipoActivo = request.IdTipoActivo,
                NumeroSerie = request.NumeroSerie,
                Marca = request.Marca,
                Modelo = request.Modelo,
                Estado = request.Estado,
                FechaRegistro = DateTime.UtcNow
            };
            if (!(entity.Estado != "asignado"))
            {
                throw new BusinessException(400, "Primero registre el activo y despues su asignacion.");
            }
            await _unitOfWork.Repository<global::HelpDesk.Domain.Models.Activos>().AddAsync(entity);
            await _unitOfWork.Complete();
            return new ResponseDto<ActivosResponseDto>(201, new ActivosResponseDto
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
            }, "Registro creado.");
        }
        catch (ValidationException ex)
        {
            return new ResponseDto<ActivosResponseDto>(400, default, ex.Message);
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