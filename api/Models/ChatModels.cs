namespace AnimatedPersona.Api.Models;

public record ChatMessage(string Id, string Role, string Content, DateTime CreatedAt);

public record AgentRunRequest(string Message, IEnumerable<ChatMessage>? Memory);

public record AgentRunResponse(string Reply, IEnumerable<ChatMessage>? Memory = null, string? SessionId = null, string? ToolLog = null);

public record MachineInfo(string MachineId, string AppName, string Region, string ServiceUrl, int Port);

public record TavusConversation(string ConversationId, string ConversationUrl, DateTime StartedAt, DateTime ExpiresAt);
