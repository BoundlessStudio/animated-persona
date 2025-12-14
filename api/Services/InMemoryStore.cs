using System.Collections.Concurrent;
using AnimatedPersona.Api.Models;

namespace AnimatedPersona.Api.Services;

public class InMemoryStore : IMemoryStore
{
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _messages = new();
    private readonly ConcurrentDictionary<string, MachineInfo> _machines = new();

    public Task ClearMachineAsync(string userId)
    {
        _machines.TryRemove(userId, out _);
        return Task.CompletedTask;
    }

    public Task<MachineInfo?> GetMachineAsync(string userId)
    {
        _machines.TryGetValue(userId, out var info);
        return Task.FromResult(info);
    }

    public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string userId)
    {
        _messages.TryGetValue(userId, out var list);
        return Task.FromResult((IReadOnlyList<ChatMessage>)(list ?? new List<ChatMessage>()));
    }

    public Task SaveMessagesAsync(string userId, IEnumerable<ChatMessage> messages)
    {
        _messages[userId] = messages.ToList();
        return Task.CompletedTask;
    }

    public Task SetMachineAsync(string userId, MachineInfo info)
    {
        _machines[userId] = info;
        return Task.CompletedTask;
    }
}
