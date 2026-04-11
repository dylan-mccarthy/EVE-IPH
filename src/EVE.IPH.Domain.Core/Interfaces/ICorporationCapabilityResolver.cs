using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

public interface ICorporationCapabilityResolver
{
    Task<Result<CorporationCapabilityState>> ResolveAsync(
        CorporationId corporationId,
        CharacterId authenticatedCharacterId,
        IReadOnlyList<string> tokenScopes,
        CancellationToken cancellationToken = default);
}

public sealed record CorporationCapabilityState(
    bool HasMembershipScope,
    bool HasAssetScope,
    bool HasIndustryJobScope,
    bool HasBlueprintScope,
    bool HasDirectorRole,
    bool HasFactoryManagerRole,
    bool HasAssetAccess,
    bool HasIndustryJobAccess,
    bool HasBlueprintAccess)
{
    public bool HasAnyCorporationScope => HasAssetScope || HasIndustryJobScope || HasBlueprintScope;

    public bool HasAnyAccess => HasAssetAccess || HasIndustryJobAccess || HasBlueprintAccess;
}