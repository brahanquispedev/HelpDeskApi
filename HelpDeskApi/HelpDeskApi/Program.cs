using HelpDesk.Api.Configuration;
using HelpDesk.Application.Configuration;
using HelpDesk.Infrastructure.Configuration;
using HelpDesk.Application.Services;
using HelpDesk.Domain.Dtos.Autenticacion;
using HelpDesk.Domain.Exceptions;

var crearAdministrador = args.Contains("--crear-admin");
var builder = WebApplication.CreateBuilder(args.Where(arg => arg != "--crear-admin").ToArray());
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();
if (crearAdministrador)
{
    using var scope = app.Services.CreateScope();
    try
    {
        await scope.ServiceProvider.GetRequiredService<IAuthService>().Bootstrap(new RegistroDto
        {
            Nombres = builder.Configuration["Bootstrap:Nombres"] ?? "Administrador",
            Apellidos = builder.Configuration["Bootstrap:Apellidos"] ?? "Inicial",
            NombreUsuario = builder.Configuration["Bootstrap:NombreUsuario"] ?? string.Empty,
            Correo = builder.Configuration["Bootstrap:Correo"] ?? string.Empty,
            Clave = builder.Configuration["Bootstrap:Clave"] ?? string.Empty
        });
        Console.WriteLine("Administrador inicial creado correctamente.");
    }
    catch (BusinessException ex)
    {
        Console.Error.WriteLine(ex.Message);
        Environment.ExitCode = 1;
    }
    catch
    {
        Console.Error.WriteLine("No se pudo crear el administrador. Revise la conexion y los datos de entrada.");
        Environment.ExitCode = 1;
    }
    return;
}
app.UseApiPipeline();
app.Run();