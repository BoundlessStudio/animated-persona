using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using AnimatedPersona.Api.Models;
using AnimatedPersona.Api.Middleware;
using AnimatedPersona.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AnimatedPersona.Api.Functions;

public class FlyFunctions
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IMemoryStore _memoryStore;
    private readonly ILogger<FlyFunctions> _logger;

    public FlyFunctions(IHttpClientFactory clientFactory, IConfiguration configuration, IMemoryStore memoryStore, ILogger<FlyFunctions> logger)
    {
        _httpClient = clientFactory.CreateClient();
        _configuration = configuration;
        _memoryStore = memoryStore;
        _logger = logger;
    }

    [Function("FlyStart")]
    public async Task<HttpResponseData> StartAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "fly/machines/start")] HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetUserSub();
        if (string.IsNullOrEmpty(user)) return req.CreateResponse(HttpStatusCode.Unauthorized);

        var app = _configuration["FLY_APP_NAME"] ?? "sample-app";
        var region = _configuration["FLY_REGION"] ?? "iad";
        var port = 3000;
        var machineId = Guid.NewGuid().ToString("N");
        var serviceUrl = $"https://{app}.fly.dev";
        var token = _configuration["FLY_API_TOKEN"];

        if (!string.IsNullOrWhiteSpace(token))
        {
            try
            {
                var createPayload = new
                {
                    config = new
                    {
                        image = "node:20-bullseye",
                        services = new[]
                        {
                            new
                            {
                                internal_port = port,
                                protocol = "tcp",
                                ports = new[] { new { port = port, handlers = new[] { "http" } } }
                            }
                        }
                    },
                    region
                };

                var message = new HttpRequestMessage(HttpMethod.Post, $"https://api.machines.dev/v1/apps/{app}/machines");
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                message.Content = new StringContent(JsonSerializer.Serialize(createPayload));
                message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = await _httpClient.SendAsync(message);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    machineId = doc.RootElement.GetProperty("id").GetString() ?? machineId;
                }
                else
                {
                    _logger.LogWarning("Fly create machine returned {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to create machine; continuing with placeholder id");
            }
        }

        var info = new MachineInfo(machineId, app, region, serviceUrl, port);
        await _memoryStore.SetMachineAsync(user, info);
        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(info);
        return res;
    }

    [Function("FlyStop")]
    public async Task<HttpResponseData> StopAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "fly/machines/stop")] HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetUserSub();
        if (string.IsNullOrEmpty(user)) return req.CreateResponse(HttpStatusCode.Unauthorized);

        var machine = await _memoryStore.GetMachineAsync(user);
        var token = _configuration["FLY_API_TOKEN"];
        var app = _configuration["FLY_APP_NAME"];

        if (machine != null && !string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(app))
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.machines.dev/v1/apps/{app}/machines/{machine.MachineId}/stop");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to stop machine");
            }
        }

        await _memoryStore.ClearMachineAsync(user);
        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteStringAsync("Machine stopped");
        return res;
    }

    [Function("FlyExec")]
    public async Task<HttpResponseData> ExecAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "fly/machines/exec")] HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetUserSub();
        if (string.IsNullOrEmpty(user)) return req.CreateResponse(HttpStatusCode.Unauthorized);
        var machine = await _memoryStore.GetMachineAsync(user);
        var token = _configuration["FLY_API_TOKEN"];
        var app = _configuration["FLY_APP_NAME"] ?? string.Empty;

        var body = await JsonSerializer.DeserializeAsync<ExecRequest>(req.Body) ?? new ExecRequest("", null, null);
        var sessionId = Guid.NewGuid().ToString("N");

        if (machine != null && !string.IsNullOrWhiteSpace(token))
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.machines.dev/v1/apps/{app}/machines/{machine.MachineId}/exec");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Content = new StringContent(JsonSerializer.Serialize(new
                {
                    cmd = body.Command,
                    env = body.Env,
                    workdir = body.Workdir
                }));
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to start exec session");
            }
        }

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new { sessionId, message = "Command started" });
        return res;
    }

    [Function("FlyExecStream")]
    public async Task<HttpResponseData> ExecStreamAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "fly/machines/exec/stream")] HttpRequestData req,
        FunctionContext context)
    {
        var res = req.CreateResponse(HttpStatusCode.OK);
        res.Headers.Add("Content-Type", "text/event-stream");
        res.Headers.Add("Cache-Control", "no-cache");

        // Simulated SSE stream to keep frontend responsive.
        var writer = new StreamWriter(res.Body);
        for (var i = 0; i < 5; i++)
        {
            await writer.WriteAsync($"data: log line {i + 1} at {DateTime.UtcNow:o}\n\n");
            await writer.FlushAsync();
            await Task.Delay(500);
        }

        return res;
    }

    private record ExecRequest(string Command, Dictionary<string, string>? Env, string? Workdir);
}
