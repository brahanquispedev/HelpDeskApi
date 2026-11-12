using HelpDesk.Application.Security;
using HelpDesk.Application.Services;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
namespace HelpDesk.Application.Configuration;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(typeof(ApplicationServicesExtensions).Assembly);
        services.AddScoped<AccessRules>();
        services.AddScoped<AuthService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ITokenService, JwtService>();
        return services;
    }
}
