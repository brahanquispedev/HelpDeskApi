using HelpDesk.Application.UseCases.EnviosNotificaciones.Queries;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.EnviosNotificaciones;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnviosNotificacionesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ResponseDto<IEnumerable<EnviosNotificacionesResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] Guid? idNotificacion, [FromQuery] string? estado, CancellationToken ct)
    {
        var result = await sender.Send(new ListarEnviosNotificacionesQuery { IdNotificacion = idNotificacion, Estado = estado }, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ResponseDto<EnviosNotificacionesResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new ObtenerEnviosNotificacionesQuery(id), ct);
        return StatusCode(result.StatusCode, result);
    }
}