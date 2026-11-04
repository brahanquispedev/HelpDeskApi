using HelpDesk.Application.Security;
using HelpDesk.Domain.Ports;
using HelpDesk.Infrastructure.Data;
using HelpDesk.Infrastructure.Repositories;
using HelpDesk.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Controladores
builder.Services.AddControllers();

// 2. Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Base de datos (PostgreSQL)
builder.Services.AddDbContext<HelpDeskDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 4. Inyección de dependencias de puertos y servicios
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<AccessRules>();
builder.Services.AddScoped<ICurrentUser, DevelopmentCurrentUser>();
builder.Services.AddScoped<IPasswordService, PasswordService>();

// 5. Registro nativo de MediatR
builder.Services.AddScoped<ServiceFactory>(p => p.GetRequiredService);
builder.Services.AddScoped<IMediator, Mediator>();
builder.Services.AddScoped<ISender>(p => p.GetRequiredService<IMediator>());
builder.Services.AddScoped<IPublisher>(p => p.GetRequiredService<IMediator>());

// Registrar automáticamente todos los IRequestHandler implementados en HelpDesk.Application
var applicationAssembly = typeof(AccessRules).Assembly;
var handlerInterfaceType = typeof(IRequestHandler<,>);

foreach (var type in applicationAssembly.GetTypes().Where(t => !t.IsAbstract && !t.IsInterface))
{
    foreach (var iface in type.GetInterfaces())
    {
        if (iface.IsGenericType && iface.GetGenericTypeDefinition() == handlerInterfaceType)
        {
            builder.Services.AddTransient(iface, type);
        }
    }
}

var app = builder.Build();

// 6. Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HelpDesk API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Mock para entorno local de pruebas
public class DevelopmentCurrentUser : ICurrentUser
{
    public Guid Id { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public string Nombre => "Dev User";
    public string Correo => "dev@helpdesk.local";
    public string Rol => "Administrador";
    public bool IsAuthenticated => true;

    public bool IsInRole(string role) => true;
    public bool HasPermission(string permission) => true;
}