using HelpDesk.Api.Middleware;
using HelpDesk.Domain.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Microsoft.Net.Http.Headers;
using System.Threading.RateLimiting;
namespace HelpDesk.Api.Configuration;

public static class ApiServicesExtensions
{
    private const string FrontendCorsPolicy = "Frontend";

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        var frontendOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy => policy
            .WithOrigins(frontendOrigins)
            .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete)
            .WithHeaders(HeaderNames.Authorization, HeaderNames.ContentType, HeaderNames.Accept)));
        services.AddControllers();
        services.Configure<ApiBehaviorOptions>(options => options.InvalidModelStateResponseFactory = context =>
            new BadRequestObjectResult(new ResponseDto<object>(400, null,
                string.Join(" ", context.ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Datos de entrada invalidos." : e.ErrorMessage)))));
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "HelpDesk API", Version = "v1" });
            options.CustomSchemaIds(SchemaId);
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
                Description = "Token obtenido en /api/Auth/login."
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        });
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "local",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0,
                    AutoReplenishment = true
                }));
        });
        services.AddHealthChecks();
        return services;
    }

    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseStatusCodePages(async context =>
        {
            var response = context.HttpContext.Response;
            await response.WriteAsJsonAsync(new ResponseDto<object>(response.StatusCode, null,
                response.StatusCode switch
                {
                    401 => "Inicie sesion con un token valido.",
                    403 => "No tiene acceso a esta operacion.",
                    404 => "Recurso no encontrado.",
                    429 => "Demasiados intentos. Espere un minuto.",
                    _ => "No se pudo completar la solicitud."
                }));
        });
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "HelpDesk API v1");
                options.RoutePrefix = string.Empty;
            });
        }
        app.UseRouting();
        app.UseCors(FrontendCorsPolicy);
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.MapControllers();
        app.MapHealthChecks("/health");
        return app;
    }

    private static string SchemaId(Type type) => type.IsGenericType
        ? type.GetGenericTypeDefinition().FullName!.Split('`')[0] + "_" + string.Join("_", type.GetGenericArguments().Select(SchemaId))
        : type.FullName!.Replace("+", ".");
}
