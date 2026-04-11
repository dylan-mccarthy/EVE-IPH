using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Manufacturing.Services;

public interface ICorporationBlueprintService
{
    Task<Result<IReadOnlyList<OwnedBlueprintRecord>>> GetAsync(
        CorporationId corporationId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<OwnedBlueprintRecord>>> RefreshAsync(
        CorporationId corporationId,
        CharacterId authenticatedCharacterId,
        CancellationToken cancellationToken = default);
}