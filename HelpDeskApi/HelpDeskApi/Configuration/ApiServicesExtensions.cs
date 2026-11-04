using HelpDesk.Api.Security;
using HelpDesk.Domain.Ports;
using Microsoft.Extensions.DependencyInjection;
namespace HelpDesk.Api.Configuration;

public static class ApiServicesExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddHealthChecks();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        return services;
    }

    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }
}
