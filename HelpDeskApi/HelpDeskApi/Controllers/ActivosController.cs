using HelpDesk.Application.UseCases.Activos.Commands;
using HelpDesk.Application.UseCases.Activos.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[ApiController, Route("api/Activos"), Authorize]
public sealed class ActivosController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ListarActivosQuery query)
    {
        var result = await mediator.Send(query);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{idActivo:guid}")]
    public async Task<IActionResult> Get(Guid idActivo)
    {
        var result = await mediator.Send(new ObtenerActivoQuery { IdActivo = idActivo });
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearActivoCommand datos)
    {
        var result = await mediator.Send(datos);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{idActivo:guid}")]
    public async Task<IActionResult> Actualizar(Guid idActivo, [FromBody] ActualizarActivoCommand datos)
    {
        datos.IdActivo = idActivo;
        var result = await mediator.Send(datos);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{idActivo:guid}/estado")]
    public async Task<IActionResult> CambiarEstado(Guid idActivo, [FromBody] CambiarEstadoActivoCommand datos)
    {
        datos.IdActivo = idActivo;
        var result = await mediator.Send(datos);
        return StatusCode(result.StatusCode, result);
    }
}
