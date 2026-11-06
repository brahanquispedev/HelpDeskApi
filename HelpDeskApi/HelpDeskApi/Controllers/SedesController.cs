using HelpDesk.Application.UseCases.Sedes.Commands;
using HelpDesk.Application.UseCases.Sedes.Queries;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.Sedes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SedesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ResponseDto<IEnumerable<SedesResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] bool? soloActivos, CancellationToken ct)
    {
        var result = await sender.Send(new ListarSedesQuery { SoloActivos = soloActivos }, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ResponseDto<SedesResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new ObtenerSedesQuery(id), ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ResponseDto<SedesResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear([FromBody] CrearSedesCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ResponseDto<SedesResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] ActualizarSedesCommand command, CancellationToken ct)
    {
        command.IdSede = id;
        var result = await sender.Send(command, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ResponseDto<OperacionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new EliminarSedesCommand(id), ct);
        return StatusCode(result.StatusCode, result);
    }
}