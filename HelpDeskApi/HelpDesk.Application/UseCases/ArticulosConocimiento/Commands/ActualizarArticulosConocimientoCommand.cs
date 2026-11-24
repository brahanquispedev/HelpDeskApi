using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Application.Services.Security;
using HelpDesk.Domain.Dtos.ArticulosConocimiento;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
namespace HelpDesk.Application.UseCases.ArticulosConocimiento.Commands;

public class ActualizarArticulosConocimientoCommand : IRequest<ResponseDto<ArticulosConocimientoResponseDto>>
{
    [JsonIgnore]
    public object[] Keys { get; set; } = [];

    public Guid IdCategoriaConocimiento { get; set; }
    [Required]
    [StringLength(10000)]
    public string Titulo { get; set; } = "";
    [Required]
    [StringLength(10000)]
    public string Contenido { get; set; } = "";
    public bool EsInterno { get; set; }
}

internal sealed class ActualizarArticulosConocimientoCommandHandler(IUnitOfWork _unitOfWork, IAccessRules access) : IRequestHandler<ActualizarArticulosConocimientoCommand, ResponseDto<ArticulosConocimientoResponseDto>>
{
    public async Task<ResponseDto<ArticulosConocimientoResponseDto>> Handle(ActualizarArticulosConocimientoCommand request, CancellationToken ct)
    {
        try
        {

            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            if (request.IdCategoriaConocimiento == Guid.Empty)
            {
                throw new ValidationException("IdCategoriaConocimiento debe ser un identificador valido.");
            }
            if (!(access.Admin))
            {
                throw new BusinessException(403, "Esta operacion requiere administracion.");
            }
            var entity = await _unitOfWork.Repository<global::HelpDesk.Domain.Models.ArticulosConocimiento>().GetByIdAsync(request.Keys)
            ?? throw new BusinessException(404, "Registro no encontrado.");
            access.EnsureRead(entity);
            if (!(entity.Estado != "publicado"))
            {
                throw new BusinessException(409, "Despublique o archive antes de editar un articulo publicado.");
            }
            entity.IdCategoriaConocimiento = request.IdCategoriaConocimiento;
            entity.Titulo = request.Titulo;
            entity.Contenido = request.Contenido;
            entity.EsInterno = request.EsInterno;

            await _unitOfWork.Complete();
            return new ResponseDto<ArticulosConocimientoResponseDto>(200, new ArticulosConocimientoResponseDto
            {
                IdArticulo = entity.IdArticulo,
                IdAutor = entity.IdAutor,
                IdRevisor = entity.IdRevisor,
                IdCategoriaConocimiento = entity.IdCategoriaConocimiento,
                Titulo = entity.Titulo,
                Contenido = entity.Contenido,
                Estado = entity.Estado,
                EsInterno = entity.EsInterno,
                FechaCreacion = entity.FechaCreacion,
                FechaPublicacion = entity.FechaPublicacion
            }, "Registro actualizado.");
        }
        catch (ValidationException ex)
        {
            return new ResponseDto<ArticulosConocimientoResponseDto>(400, default, ex.Message);
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<ArticulosConocimientoResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<ArticulosConocimientoResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }

}