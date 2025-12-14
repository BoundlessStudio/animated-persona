using System.Text.Json;
using AnimatedPersona.Api.Models;
using Azure;
using Azure.Data.Tables;

namespace AnimatedPersona.Api.Services;

public class TableMemoryStore : IMemoryStore
{
    private readonly TableClient _tableClient;

    public TableMemoryStore(string connectionString, string tableName)
    {
        _tableClient = new TableClient(connectionString, tableName);
        _tableClient.CreateIfNotExists();
    }

    public async Task ClearMachineAsync(string userId)
    {
        try
        {
            await _tableClient.DeleteEntityAsync(userId, "machine", ETag.All, CancellationToken.None);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Treat missing entries as already cleared to mirror in-memory behavior
            return;
        }
    }

    public async Task<MachineInfo?> GetMachineAsync(string userId)
    {
        try
        {
            var entity = await _tableClient.GetEntityAsync<TableEntity>(userId, "machine");
            if (entity?.Value == null) return null;
            if (!entity.Value.TryGetValue("Payload", out var payloadProp)) return null;
            var payload = payloadProp?.ToString();
            if (string.IsNullOrWhiteSpace(payload)) return null;
            try
            {
                return JsonSerializer.Deserialize<MachineInfo>(payload);
            }
            catch (JsonException)
            {
                return null;
            }
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string userId)
    {
        var query = _tableClient.QueryAsync<TableEntity>(t => t.PartitionKey == userId && t.RowKey.StartsWith("msg-"));
        var results = new List<ChatMessage>();
        await foreach (var entity in query)
        {
            var payload = entity.GetString("Payload");
            if (!string.IsNullOrWhiteSpace(payload))
            {
                var msg = JsonSerializer.Deserialize<ChatMessage>(payload);
                if (msg != null)
                {
                    results.Add(msg);
                }
            }
        }

        return results.OrderBy(m => m.CreatedAt).ToList();
    }

    public async Task SaveMessagesAsync(string userId, IEnumerable<ChatMessage> messages)
    {
        var batch = new List<TableTransactionAction>();
        var index = 0;
        foreach (var message in messages)
        {
            var entity = new TableEntity(userId, $"msg-{index:D4}")
            {
                { "Payload", JsonSerializer.Serialize(message) }
            };
            batch.Add(new TableTransactionAction(TableTransactionActionType.UpsertReplace, entity));
            index++;
        }

        // Remove old entries first
        await PurgePartitionAsync(userId, prefix: "msg-");
        if (batch.Count > 0)
        {
            foreach (var chunk in batch.Chunk(100))
            {
                await _tableClient.SubmitTransactionAsync(chunk);
            }
        }
    }

    public async Task SetMachineAsync(string userId, MachineInfo info)
    {
        var entity = new TableEntity(userId, "machine")
        {
            { "Payload", JsonSerializer.Serialize(info) }
        };
        await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
    }

    private async Task PurgePartitionAsync(string partitionKey, string? prefix = null)
    {
        var query = _tableClient.QueryAsync<TableEntity>(t => t.PartitionKey == partitionKey && (prefix == null || t.RowKey.StartsWith(prefix)));
        await foreach (var entity in query)
        {
            await _tableClient.DeleteEntityAsync(partitionKey, entity.RowKey);
        }
    }
}
