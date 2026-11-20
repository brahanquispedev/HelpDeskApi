using HelpDesk.Application.UseCases.Autenticacion.Commands;
using HelpDesk.Domain.Dtos.Autenticacion;
using HelpDesk.Domain.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HelpDesk.Api.Controllers;

[ApiController, Route("api/Auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpGet("sesion"), Authorize]
    public ActionResult<ResponseDto<SesionActualDto>> Sesion()
    {
        var roles = User.FindAll("role").Select(claim => claim.Value).Distinct().ToArray();
        var permisos = User.FindAll("permiso").Select(claim => claim.Value).Distinct().ToArray();
        return Ok(new ResponseDto<SesionActualDto>(200, new SesionActualDto(roles, permisos)));
    }

    [HttpPost("registro"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<IActionResult> Registro([FromBody] RegistrarUsuarioCommand datos)
    {
        var result = await mediator.Send(datos);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] IniciarSesionCommand datos)
    {
        var result = await mediator.Send(datos);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("cambiar-clave"), Authorize]
    public async Task<IActionResult> CambiarClave([FromBody] CambiarClaveCommand datos)
    {
        var result = await mediator.Send(datos);
        return StatusCode(result.StatusCode, result);
    }
}
