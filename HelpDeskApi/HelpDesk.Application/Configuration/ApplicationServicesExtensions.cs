using HelpDesk.Application.Security;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
namespace HelpDesk.Application.Configuration;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(typeof(ApplicationServicesExtensions).Assembly);
        services.AddScoped<AccessRules>();
        return services;
    }
}
