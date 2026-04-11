using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class NullCharacterResearchAgentDataSource : ICharacterResearchAgentDataSource
{
    public Task<Result<IReadOnlyList<CharacterResearchAgentData>>> GetResearchAgentsAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(characterId);

        return Task.FromResult(
            Result<IReadOnlyList<CharacterResearchAgentData>>.Failure(
                "NOT_IMPLEMENTED",
                "Research-agent refresh is not wired into the Avalonia host yet."));
    }
}