using HelpDesk.Application.Common;
using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Activos;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using ActivoEntity = HelpDesk.Domain.Models.Activos;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.UseCases.Activos.Queries;

public class ListarActivosQuery : IRequest<ResponseDto<PaginaDto<ActivosResponseDto>>>
{
    public int Pagina { get; set; } = 1;
    public int Tamano { get; set; } = 20;
    public string? Buscar { get; set; }
    public Guid? IdSede { get; set; }
    public Guid? IdArea { get; set; }
    public Guid? IdTipoActivo { get; set; }
    public string? Estado { get; set; }
}

internal sealed class ListarActivosQueryHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<ListarActivosQuery, ResponseDto<PaginaDto<ActivosResponseDto>>>
{
    public async Task<ResponseDto<PaginaDto<ActivosResponseDto>>> Handle(ListarActivosQuery request, CancellationToken ct)
    {
        try
        {
            Validation.Require(request.Pagina >= 1 && request.Pagina <= 1000000 && request.Tamano is >= 1 and <= 100,
                "Pagina debe ser positiva y tamano debe estar entre 1 y 100.");

            var query = access.Scope(_unitOfWork.Repository<ActivoEntity>().Query());

            if (!string.IsNullOrWhiteSpace(request.Buscar))
            {
                var texto = request.Buscar.Trim().ToLower();
                query = query.Where(x =>
                    x.CodigoPatrimonial.ToLower().Contains(texto) ||
                    x.Nombre.ToLower().Contains(texto) ||
                    (x.NumeroSerie != null && x.NumeroSerie.ToLower().Contains(texto)));
            }

            if (request.IdSede.HasValue)
                query = query.Where(x => x.IdSede == request.IdSede.Value);

            if (request.IdArea.HasValue)
                query = query.Where(x => x.IdArea == request.IdArea.Value);

            if (request.IdTipoActivo.HasValue)
                query = query.Where(x => x.IdTipoActivo == request.IdTipoActivo.Value);

            if (!string.IsNullOrWhiteSpace(request.Estado))
            {
                var estado = request.Estado.Trim().ToLower();
                query = query.Where(x => x.Estado == estado);
            }

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderBy(x => x.Nombre)
                .Skip((request.Pagina - 1) * request.Tamano)
                .Take(request.Tamano)
                .Select(entity => new ActivosResponseDto
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
                })
                .ToListAsync(ct);

            return new ResponseDto<PaginaDto<ActivosResponseDto>>(200,
                new PaginaDto<ActivosResponseDto>(items, request.Pagina, request.Tamano, total));
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<PaginaDto<ActivosResponseDto>>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<PaginaDto<ActivosResponseDto>>(500, default, "No se pudo completar la operacion.");
        }
    }
}
