using server.Models;

namespace server.Services.Characters;

public interface ICharacterSyncService
{
    Task<CharacterSyncResponse> SyncCharacterDataAsync(long characterId, CancellationToken ct = default);
}
