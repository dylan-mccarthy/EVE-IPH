using server.Models;

namespace server.Services.Characters;

public interface ICharacterService
{
    Task<CharacterListResponse> GetCharactersAsync(CancellationToken ct = default);
    Task<CharacterDetails> GetCharacterDetailsAsync(long characterId, CancellationToken ct = default);
    Task<CharacterProfile> GetProfileAsync(long characterId, string accessToken, CancellationToken ct = default);
    Task<CharacterSkillsResponse> GetSkillsAsync(long characterId, string accessToken, CancellationToken ct = default);
}
