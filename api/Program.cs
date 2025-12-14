using System.Text.Json;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AnimatedPersona.Api.Middleware;
using AnimatedPersona.Api.Services;
using AnimatedPersona.Api.Options;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<AuthMiddleware>();
builder.Services.AddSingleton<TokenValidator>();
builder.Services.AddSingleton<IMemoryStore>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var conn = cfg["TABLE_STORAGE_CONNECTION_STRING"] ?? cfg["AZURE_TABLES_CONNECTION_STRING"];
    if (!string.IsNullOrWhiteSpace(conn))
    {
        return new TableMemoryStore(conn, cfg["TABLE_STORAGE_TABLE"] ?? "agentmemory");
    }
    return new InMemoryStore();
});
builder.Services.Configure<JwtValidationOptions>(options =>
{
    var cfg = builder.Configuration;
    options.Audience = cfg["AUTH0_AUDIENCE"] ?? string.Empty;
    options.Authority = cfg["AUTH0_DOMAIN"] ?? string.Empty;
});

builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.WriteIndented = true;
});

builder.UseMiddleware<AuthMiddleware>();

builder.Build().Run();
