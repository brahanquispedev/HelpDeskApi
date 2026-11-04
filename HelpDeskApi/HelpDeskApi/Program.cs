using HelpDesk.Api.Configuration;
using HelpDesk.Application.Configuration;
using HelpDesk.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApiServices();

var app = builder.Build();
app.UseApiPipeline();
app.Run();
