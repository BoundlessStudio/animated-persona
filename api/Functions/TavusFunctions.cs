using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AnimatedPersona.Api.Models;
using AnimatedPersona.Api.Middleware;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AnimatedPersona.Api.Functions;

public class TavusFunctions
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TavusFunctions> _logger;

    public TavusFunctions(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<TavusFunctions> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _configuration = configuration;
        _logger = logger;
    }

    [Function("StartConversation")]
    public async Task<HttpResponseData> StartConversationAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tavus/conversations/start")] HttpRequestData req,
        FunctionContext context)
    {
        var sub = context.GetUserSub();
        if (string.IsNullOrEmpty(sub))
        {
            return req.CreateResponse(HttpStatusCode.Unauthorized);
        }

        var apiKey = _configuration["TAVUS_API_KEY"];
        var personaId = _configuration["TAVUS_PERSONA_ID"];
        var replicaId = _configuration["TAVUS_REPLICA_ID"];

        TavusConversation conversation;
        try
        {
            if (!string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(personaId))
            {
                var payload = new
                {
                    persona_id = personaId,
                    replica_id = replicaId,
                    metadata = new { user = sub }
                };
                var message = new HttpRequestMessage(HttpMethod.Post, "https://api.tavus.io/v2/conversations")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload))
                };
                message.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                message.Headers.Add("x-api-key", apiKey);
                var response = await _httpClient.SendAsync(message);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    var id = root.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
                    var url = root.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
                    var started = DateTime.UtcNow;
                    var expires = started.AddMinutes(60);
                    conversation = new TavusConversation(id, url ?? $"https://app.tavus.io/conversations/{id}", started, expires);
                }
                else
                {
                    _logger.LogWarning("Tavus API returned {StatusCode}", response.StatusCode);
                    conversation = BuildFallbackConversation();
                }
            }
            else
            {
                conversation = BuildFallbackConversation();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Tavus conversation");
            conversation = BuildFallbackConversation();
        }

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new
        {
            conversationId = conversation.ConversationId,
            conversationUrl = conversation.ConversationUrl,
            startedAt = conversation.StartedAt,
            expiresAt = conversation.ExpiresAt
        });
        return res;
    }

    [Function("EndConversation")]
    public async Task<HttpResponseData> EndConversationAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tavus/conversations/{id}/end")] HttpRequestData req,
        FunctionContext context,
        string id)
    {
        var sub = context.GetUserSub();
        if (string.IsNullOrEmpty(sub))
        {
            return req.CreateResponse(HttpStatusCode.Unauthorized);
        }

        var apiKey = _configuration["TAVUS_API_KEY"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.tavus.io/v2/conversations/{id}/end");
                request.Headers.Add("x-api-key", apiKey);
                await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not call Tavus end endpoint; falling back to client-side end");
            }
        }

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteStringAsync("Conversation ended or marked as complete.");
        return res;
    }

    private TavusConversation BuildFallbackConversation()
    {
        var id = Guid.NewGuid().ToString();
        var started = DateTime.UtcNow;
        return new TavusConversation(id, $"https://app.tavus.io/conversations/{id}", started, started.AddMinutes(60));
    }
}
