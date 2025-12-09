using server.Models;

namespace server.Services.Characters;

public interface ICharacterService
{
    Task<CharacterProfile> GetProfileAsync(long characterId, string accessToken, CancellationToken ct = default);
    Task<CharacterSkillsResponse> GetSkillsAsync(long characterId, string accessToken, CancellationToken ct = default);
}
