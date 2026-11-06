using HelpDesk.Application.UseCases.Autenticacion;
using HelpDesk.Domain.Dtos.Autenticacion;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[ApiController, Route("api/Auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("registro"), AllowAnonymous]
    public async Task<IActionResult> Registro([FromBody] RegistrarUsuarioCommand datos)
    {
        var result = await mediator.Send(datos);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("login"), AllowAnonymous]
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
