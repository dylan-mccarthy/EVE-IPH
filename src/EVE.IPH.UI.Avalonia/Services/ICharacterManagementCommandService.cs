using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.UI.Avalonia.Services;

public interface ICharacterManagementCommandService
{
    Task<Result<CharacterRecord>> AuthenticateAndRefreshAsync(CancellationToken cancellationToken = default);

    Task<Result<CharacterRecord>> RefreshAsync(CharacterId characterId, CancellationToken cancellationToken = default);

    Task<Result<CorporationConnectionRecord>> ConnectCorporationAsync(CharacterId characterId, CancellationToken cancellationToken = default);

    Task<Result<CorporationConnectionRecord>> RefreshCorporationAsync(CorporationId corporationId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CharacterRecord>>> SetDefaultAsync(CharacterId characterId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CharacterRecord>>> DeleteAsync(CharacterId characterId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CorporationConnectionRecord>>> DeleteCorporationAsync(CorporationId corporationId, CancellationToken cancellationToken = default);
}