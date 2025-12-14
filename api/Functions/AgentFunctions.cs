using System.Net;
using System.Text.Json;
using AnimatedPersona.Api.Models;
using AnimatedPersona.Api.Middleware;
using AnimatedPersona.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AnimatedPersona.Api.Functions;

public class AgentFunctions
{
    private readonly IMemoryStore _memoryStore;
    private readonly ILogger<AgentFunctions> _logger;

    public AgentFunctions(IMemoryStore memoryStore, ILogger<AgentFunctions> logger)
    {
        _memoryStore = memoryStore;
        _logger = logger;
    }

    [Function("AgentRun")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "agent/run")] HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetUserSub();
        if (string.IsNullOrEmpty(user)) return req.CreateResponse(HttpStatusCode.Unauthorized);

        var payload = await JsonSerializer.DeserializeAsync<AgentRunRequest>(req.Body) ?? new AgentRunRequest(string.Empty, null);
        var history = (await _memoryStore.GetMessagesAsync(user)).ToList();
        if (payload.Memory != null)
        {
            history = payload.Memory.ToList();
        }

        var replyText = $"Agent received: '{payload.Message}'. Fly exec session will stream logs when available.";
        var assistantMessage = new ChatMessage(Guid.NewGuid().ToString(), "assistant", replyText, DateTime.UtcNow);
        history.Add(new ChatMessage(Guid.NewGuid().ToString(), "user", payload.Message, DateTime.UtcNow));
        history.Add(assistantMessage);

        await _memoryStore.SaveMessagesAsync(user, history);

        var response = req.CreateResponse(HttpStatusCode.OK);
        var sessionId = Guid.NewGuid().ToString("N");
        var toolLog = "npm install && npm run dev (simulated)";
        await response.WriteAsJsonAsync(new AgentRunResponse(assistantMessage.Content, history, sessionId, toolLog));
        return response;
    }
}
