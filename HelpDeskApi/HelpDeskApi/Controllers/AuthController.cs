using HelpDesk.Application.UseCases.Autenticacion.Commands;
using HelpDesk.Domain.Dtos.Autenticacion;
using HelpDesk.Application.Security;
using HelpDesk.Application.UseCases.Autenticacion;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace HelpDesk.Api.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("registro"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegistrarUsuarioCommand datos)
    {
        var r = await mediator.Send(datos); return StatusCode(r.StatusCode, r);
    }
    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] IniciarSesionCommand datos)
    {
        var r = await mediator.Send(datos); return StatusCode(r.StatusCode, r);
    }
    [HttpPost("cambiar-clave"), Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] CambiarClaveCommand datos)
    {
        var r = await mediator.Send(datos); return StatusCode(r.StatusCode, r);
    }


}