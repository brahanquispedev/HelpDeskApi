using HelpDesk.Application.UseCases.Autenticacion;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HelpDesk.Api.Controllers;

[ApiController, Route("api/Autenticacion")]
public sealed class AutenticacionController(IMediator mediator) : ControllerBase
{
    [HttpPost("registro"), AllowAnonymous]
    public async Task<IActionResult> Registro([FromBody] RegistrarUsuarioCommand datos)
    {
        var result = await mediator.Send(datos);
        return StatusCode(result.StatusCode, result);
    }
}
