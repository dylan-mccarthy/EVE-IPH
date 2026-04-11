using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.UI.Avalonia.Services;

public interface ICharacterManagementQueryService
{
    Task<Result<CharacterManagementScreenData>> GetScreenDataAsync(CancellationToken cancellationToken = default);
}

public sealed record CharacterManagementScreenData(
    IReadOnlyList<CharacterManagementCharacterRow> Characters,
    IReadOnlyList<CharacterManagementCorporationRow> Corporations,
    string StatusText);

public sealed record CharacterManagementCharacterRow(
    CharacterRecord Character,
    CharacterTokenStatus TokenStatus);

public sealed record CharacterManagementCorporationRow(
    CorporationConnectionRecord Corporation,
    string AuthorizedCharacterName);

public sealed record CharacterTokenStatus(
    CharacterId CharacterId,
    bool HasStoredToken,
    bool IsExpired,
    DateTimeOffset? ExpiresAtUtc,
    string StatusText,
    IReadOnlyList<string> Scopes);