using HelpDesk.Application.UseCases.Areas.Commands;
using HelpDesk.Application.UseCases.Areas.Queries;
using HelpDesk.Domain.Dtos.Areas;
using HelpDesk.Domain.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AreasController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ResponseDto<IEnumerable<AreasResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] Guid? idSede, [FromQuery] bool? soloActivos, CancellationToken ct)
    {
        var result = await sender.Send(new ListarAreasQuery { IdSede = idSede, SoloActivos = soloActivos }, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ResponseDto<AreasResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new ObtenerAreasQuery(id), ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ResponseDto<AreasResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear([FromBody] CrearAreasCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ResponseDto<AreasResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] ActualizarAreasCommand command, CancellationToken ct)
    {
        command.IdArea = id;
        var result = await sender.Send(command, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ResponseDto<OperacionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new EliminarAreasCommand(id), ct);
        return StatusCode(result.StatusCode, result);
    }
}