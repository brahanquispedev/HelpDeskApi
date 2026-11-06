using HelpDesk.Application.UseCases.PreferenciasNotificaciones.Commands;
using HelpDesk.Application.UseCases.PreferenciasNotificaciones.Queries;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.PreferenciasNotificaciones;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PreferenciasNotificacionesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ResponseDto<IEnumerable<PreferenciasNotificacionesResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] Guid? idUsuario, CancellationToken ct)
    {
        var result = await sender.Send(new ListarPreferenciasNotificacionesQuery { IdUsuario = idUsuario }, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{idUsuario:guid}/{tipoEvento}")]
    [ProducesResponseType(typeof(ResponseDto<PreferenciasNotificacionesResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid idUsuario, string tipoEvento, CancellationToken ct)
    {
        var result = await sender.Send(new ObtenerPreferenciasNotificacionesQuery(idUsuario, tipoEvento), ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ResponseDto<PreferenciasNotificacionesResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear([FromBody] CrearPreferenciasNotificacionesCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{idUsuario:guid}/{tipoEvento}")]
    [ProducesResponseType(typeof(ResponseDto<PreferenciasNotificacionesResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(Guid idUsuario, string tipoEvento, [FromBody] ActualizarPreferenciasNotificacionesCommand command, CancellationToken ct)
    {
        command.IdUsuario = idUsuario;
        command.TipoEvento = tipoEvento;
        var result = await sender.Send(command, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{idUsuario:guid}/{tipoEvento}")]
    [ProducesResponseType(typeof(ResponseDto<OperacionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid idUsuario, string tipoEvento, CancellationToken ct)
    {
        var result = await sender.Send(new EliminarPreferenciasNotificacionesCommand(idUsuario, tipoEvento), ct);
        return StatusCode(result.StatusCode, result);
    }
}