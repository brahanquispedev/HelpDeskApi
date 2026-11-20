using HelpDesk.Api.Security;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
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
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("auth", opt =>
            {
                opt.PermitLimit = 5;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 0;
            });
        });
        return services;
    }

    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseRouting();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/health");
        return app;
    }
}
