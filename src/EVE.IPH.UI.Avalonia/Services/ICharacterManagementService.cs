using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.UI.Avalonia.Services;

public interface ICharacterManagementService
{
    Task<Result<IReadOnlyList<CharacterRecord>>> GetCharactersAsync(CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CharacterTokenStatus>>> GetCharacterTokenStatusesAsync(CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CorporationConnectionRecord>>> GetCorporationsAsync(CancellationToken cancellationToken = default);

    Task<Result<CharacterRecord>> AuthenticateAndRefreshAsync(CancellationToken cancellationToken = default);

    Task<Result<CharacterRecord>> RefreshAsync(CharacterId characterId, CancellationToken cancellationToken = default);

    Task<Result<CorporationConnectionRecord>> ConnectCorporationAsync(CharacterId characterId, CancellationToken cancellationToken = default);

    Task<Result<CorporationConnectionRecord>> RefreshCorporationAsync(CorporationId corporationId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CharacterRecord>>> SetDefaultAsync(CharacterId characterId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CharacterRecord>>> DeleteAsync(CharacterId characterId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CorporationConnectionRecord>>> DeleteCorporationAsync(CorporationId corporationId, CancellationToken cancellationToken = default);
}

public sealed record CharacterTokenStatus(
    CharacterId CharacterId,
    bool HasStoredToken,
    bool IsExpired,
    DateTimeOffset? ExpiresAtUtc,
    string StatusText,
    IReadOnlyList<string> Scopes);