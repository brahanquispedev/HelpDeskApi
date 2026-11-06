using HelpDesk.Application.UseCases.Notificaciones.Commands;
using HelpDesk.Domain.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskApi.Controllers;

[ApiController]
[Route("api/notificaciones")]
[Authorize]
public class NotificacionesAccionesController(ISender sender) : ControllerBase
{
    [HttpPost("{id:guid}/marcar-leida")]
    [ProducesResponseType(typeof(ResponseDto<OperacionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarcarLeida(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new MarcarNotificacionLeidaCommand(id), ct);
        return StatusCode(result.StatusCode, result);
    }
}