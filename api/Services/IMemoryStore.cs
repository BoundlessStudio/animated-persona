using AnimatedPersona.Api.Models;

namespace AnimatedPersona.Api.Services;

public interface IMemoryStore
{
    Task SaveMessagesAsync(string userId, IEnumerable<ChatMessage> messages);
    Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string userId);
    Task SetMachineAsync(string userId, MachineInfo info);
    Task<MachineInfo?> GetMachineAsync(string userId);
    Task ClearMachineAsync(string userId);
}
