using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Application.Services.Security;
using HelpDesk.Domain.Dtos.Problemas;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
namespace HelpDesk.Application.UseCases.Problemas.Commands;

public class CrearProblemasCommand : IRequest<ResponseDto<ProblemasResponseDto>>
{
    public Guid IdResponsable { get; set; }
    [Required]
    [StringLength(10000)]
    public string Titulo { get; set; } = "";
    [Required]
    [StringLength(10000)]
    public string Descripcion { get; set; } = "";
    [Required]
    [StringLength(10000)]
    public string Estado { get; set; } = "abierto";
    public string? CausaRaiz { get; set; }
    public string? SolucionTemporal { get; set; }
    public string? SolucionDefinitiva { get; set; }
}

internal sealed class CrearProblemasCommandHandler(IUnitOfWork _unitOfWork, AccessRules access) : IRequestHandler<CrearProblemasCommand, ResponseDto<ProblemasResponseDto>>
{
    public async Task<ResponseDto<ProblemasResponseDto>> Handle(CrearProblemasCommand request, CancellationToken ct)
    {
        try
        {

            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            if (request.IdResponsable == Guid.Empty)
            {
                throw new ValidationException("IdResponsable debe ser un identificador valido.");
            }
            if (!(access.Admin))
            {
                throw new BusinessException(403, "Esta operacion requiere administracion.");
            }
            var entity = new global::HelpDesk.Domain.Models.Problemas()
            {
                IdProblema = Guid.NewGuid(),
                IdResponsable = request.IdResponsable,
                Titulo = request.Titulo,
                Descripcion = request.Descripcion,
                Estado = request.Estado,
                CausaRaiz = request.CausaRaiz,
                SolucionTemporal = request.SolucionTemporal,
                SolucionDefinitiva = request.SolucionDefinitiva,
                FechaCreacion = DateTime.UtcNow
            };

            await _unitOfWork.Repository<global::HelpDesk.Domain.Models.Problemas>().AddAsync(entity);
            await _unitOfWork.Complete();
            return new ResponseDto<ProblemasResponseDto>(201, new ProblemasResponseDto
            {
                IdProblema = entity.IdProblema,
                IdResponsable = entity.IdResponsable,
                Titulo = entity.Titulo,
                Descripcion = entity.Descripcion,
                Estado = entity.Estado,
                CausaRaiz = entity.CausaRaiz,
                SolucionTemporal = entity.SolucionTemporal,
                SolucionDefinitiva = entity.SolucionDefinitiva,
                FechaCreacion = entity.FechaCreacion
            }, "Registro creado.");
        }
        catch (ValidationException ex)
        {
            return new ResponseDto<ProblemasResponseDto>(400, default, ex.Message);
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<ProblemasResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<ProblemasResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }

}
