using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Interfaces;

namespace EVE.IPH.Infrastructure.ESI;

public sealed class EsiCorporationCapabilityResolver(IEsiClient esiClient) : ICorporationCapabilityResolver
{
    private const string CorporationMembershipScope = "esi-corporations.read_corporation_membership";
    private const string CorporationAssetScope = "esi-assets.read_corporation_assets";
    private const string CorporationIndustryJobScope = "esi-industry.read_corporation_jobs";
    private const string CorporationBlueprintScope = "esi-corporations.read_blueprints";
    private const string DirectorRole = "Director";
    private const string FactoryManagerRole = "Factory_Manager";

    private readonly IEsiClient _esiClient = esiClient ?? throw new ArgumentNullException(nameof(esiClient));

    public async Task<Result<CorporationCapabilityState>> ResolveAsync(
        CorporationId corporationId,
        CharacterId authenticatedCharacterId,
        IReadOnlyList<string> tokenScopes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tokenScopes);

        bool hasMembershipScope = ContainsScope(tokenScopes, CorporationMembershipScope);
        bool hasAssetScope = ContainsScope(tokenScopes, CorporationAssetScope);
        bool hasIndustryJobScope = ContainsScope(tokenScopes, CorporationIndustryJobScope);
        bool hasBlueprintScope = ContainsScope(tokenScopes, CorporationBlueprintScope);

        if (!hasAssetScope && !hasIndustryJobScope && !hasBlueprintScope)
        {
            return Result<CorporationCapabilityState>.Success(new CorporationCapabilityState(
                hasMembershipScope,
                hasAssetScope,
                hasIndustryJobScope,
                hasBlueprintScope,
                false,
                false,
                false,
                false,
                false));
        }

        if (!hasMembershipScope)
        {
            return Result<CorporationCapabilityState>.Success(new CorporationCapabilityState(
                hasMembershipScope,
                hasAssetScope,
                hasIndustryJobScope,
                hasBlueprintScope,
                false,
                false,
                false,
                false,
                false));
        }

        Result<IReadOnlyList<string>> rolesResult = await _esiClient
            .GetCorporationRolesAsync(corporationId, authenticatedCharacterId, cancellationToken)
            .ConfigureAwait(false);

        if (rolesResult.IsFailure)
        {
            if (ShouldTreatAsNoRoles(rolesResult.Error))
            {
                return Result<CorporationCapabilityState>.Success(new CorporationCapabilityState(
                    hasMembershipScope,
                    hasAssetScope,
                    hasIndustryJobScope,
                    hasBlueprintScope,
                    false,
                    false,
                    false,
                    false,
                    false));
            }

            return Result<CorporationCapabilityState>.Failure(rolesResult.Error);
        }

        bool hasDirectorRole = rolesResult.Value.Contains(DirectorRole, StringComparer.OrdinalIgnoreCase);
        bool hasFactoryManagerRole = rolesResult.Value.Contains(FactoryManagerRole, StringComparer.OrdinalIgnoreCase);

        return Result<CorporationCapabilityState>.Success(new CorporationCapabilityState(
            hasMembershipScope,
            hasAssetScope,
            hasIndustryJobScope,
            hasBlueprintScope,
            hasDirectorRole,
            hasFactoryManagerRole,
            hasAssetScope && hasDirectorRole,
            hasIndustryJobScope && hasFactoryManagerRole,
            hasBlueprintScope && hasDirectorRole));
    }

    private static bool ContainsScope(IReadOnlyList<string> scopes, string expectedScope) =>
        scopes.Contains(expectedScope, StringComparer.OrdinalIgnoreCase) ||
        scopes.Contains($"{expectedScope}.v1", StringComparer.OrdinalIgnoreCase);

    private static bool ShouldTreatAsNoRoles(Error error)
    {
        if (error.Code is "ESI_403" or "ESI_404")
        {
            return true;
        }

        string message = error.Message;
        return message.Contains("Character not in corporation", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Character cannot grant roles", StringComparison.OrdinalIgnoreCase)
            || message.Contains("required role(s)", StringComparison.OrdinalIgnoreCase)
            || message.Contains("corporation_id", StringComparison.OrdinalIgnoreCase);
    }
}