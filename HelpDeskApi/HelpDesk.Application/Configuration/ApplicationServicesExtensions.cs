using HelpDesk.Application.Services.Security;
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
        return services;
    }
}