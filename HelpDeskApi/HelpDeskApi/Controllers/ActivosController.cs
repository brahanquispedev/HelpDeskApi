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
}
