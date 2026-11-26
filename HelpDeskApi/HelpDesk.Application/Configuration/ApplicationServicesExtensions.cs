using HelpDesk.Application.Services.Security;
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
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IAccessRules, AccessRules>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITicketNotifications, TicketNotifications>();
        services.AddScoped<ITokenService, JwtService>();
        return services;
    }
}