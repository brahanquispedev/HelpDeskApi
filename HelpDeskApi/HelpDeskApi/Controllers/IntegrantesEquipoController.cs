using HelpDesk.Application.UseCases.IntegrantesEquipo.Commands;
using HelpDesk.Application.UseCases.IntegrantesEquipo.Queries;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.IntegrantesEquipo;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IntegrantesEquipoController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ResponseDto<IEnumerable<IntegrantesEquipoResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] Guid? idEquipo, [FromQuery] bool? soloActivos, CancellationToken ct)
    {
        var result = await sender.Send(new ListarIntegrantesEquipoQuery { IdEquipo = idEquipo, SoloActivos = soloActivos }, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{idEquipo:guid}/{idUsuario:guid}")]
    [ProducesResponseType(typeof(ResponseDto<IntegrantesEquipoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid idEquipo, Guid idUsuario, CancellationToken ct)
    {
        var result = await sender.Send(new ObtenerIntegrantesEquipoQuery(idEquipo, idUsuario), ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ResponseDto<IntegrantesEquipoResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear([FromBody] CrearIntegrantesEquipoCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{idEquipo:guid}/{idUsuario:guid}")]
    [ProducesResponseType(typeof(ResponseDto<IntegrantesEquipoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(Guid idEquipo, Guid idUsuario, [FromBody] ActualizarIntegrantesEquipoCommand command, CancellationToken ct)
    {
        command.IdEquipo = idEquipo;
        command.IdUsuario = idUsuario;
        var result = await sender.Send(command, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{idEquipo:guid}/{idUsuario:guid}")]
    [ProducesResponseType(typeof(ResponseDto<OperacionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid idEquipo, Guid idUsuario, CancellationToken ct)
    {
        var result = await sender.Send(new EliminarIntegrantesEquipoCommand(idEquipo, idUsuario), ct);
        return StatusCode(result.StatusCode, result);
    }
}