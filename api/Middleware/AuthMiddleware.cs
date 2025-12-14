using System.Net;
using System.Security.Claims;
using System.Text.Json;
using AnimatedPersona.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace AnimatedPersona.Api.Middleware;

public class AuthMiddleware : IFunctionsWorkerMiddleware
{
    private readonly TokenValidator _validator;
    private readonly ILogger<AuthMiddleware> _logger;

    public AuthMiddleware(TokenValidator validator, ILogger<AuthMiddleware> logger)
    {
        _validator = validator;
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var request = await context.GetHttpRequestDataAsync();
        if (request == null)
        {
            await next(context);
            return;
        }

        if (string.Equals(request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (!request.Headers.TryGetValues("Authorization", out var values))
        {
            await WriteUnauthorizedAsync(request, context, "Missing Authorization header");
            return;
        }

        var bearer = values.FirstOrDefault();
        if (!System.Net.Http.Headers.AuthenticationHeaderValue.TryParse(bearer, out var parsed) ||
            !string.Equals(parsed.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(parsed.Parameter))
        {
            await WriteUnauthorizedAsync(request, context, "Invalid bearer token");
            return;
        }

        var principal = await _validator.ValidateAsync(parsed.Parameter);
        if (principal == null)
        {
            await WriteUnauthorizedAsync(request, context, "Token validation failed");
            return;
        }

        context.Items["User"] = principal;
        await next(context);
    }

    private static async Task WriteUnauthorizedAsync(HttpRequestData request, FunctionContext context, string message)
    {
        var response = request.CreateResponse(HttpStatusCode.Unauthorized);
        await response.WriteStringAsync(JsonSerializer.Serialize(new { error = message }));
        context.GetInvocationResult().Value = response;
    }
}

public static class FunctionContextExtensions
{
    public static ClaimsPrincipal? GetUser(this FunctionContext context)
    {
        return context.Items.TryGetValue("User", out var principal) ? principal as ClaimsPrincipal : null;
    }

    public static string? GetUserSub(this FunctionContext context)
    {
        return context.GetUser()?.FindFirst("sub")?.Value;
    }
}
