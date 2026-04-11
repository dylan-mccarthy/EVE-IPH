using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Interfaces;
using NSubstitute;

namespace EVE.IPH.Infrastructure.ESI.Tests;

public sealed class EsiCorporationCapabilityResolverTests
{
    [Fact]
    public async Task ResolveAsync_WhenDirectorAndFactoryManagerRolesExist_GrantsExpectedCapabilities()
    {
        IEsiClient esiClient = Substitute.For<IEsiClient>();
        CorporationId corporationId = new(98000001);
        CharacterId characterId = new(90000001);

        esiClient.GetCorporationRolesAsync(corporationId, characterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<string>>.Success(["Director", "Factory_Manager"])));

        EsiCorporationCapabilityResolver sut = new(esiClient);

        var result = await sut.ResolveAsync(
            corporationId,
            characterId,
            ["esi-assets.read_corporation_assets", "esi-industry.read_corporation_jobs", "esi-corporations.read_blueprints", "esi-corporations.read_corporation_membership"]);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasDirectorRole.Should().BeTrue();
        result.Value.HasFactoryManagerRole.Should().BeTrue();
        result.Value.HasAssetAccess.Should().BeTrue();
        result.Value.HasIndustryJobAccess.Should().BeTrue();
        result.Value.HasBlueprintAccess.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveAsync_WhenRoleLookupReturnsForbidden_TreatsCapabilitiesAsMissing()
    {
        IEsiClient esiClient = Substitute.For<IEsiClient>();
        CorporationId corporationId = new(98000001);
        CharacterId characterId = new(90000001);

        esiClient.GetCorporationRolesAsync(corporationId, characterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<string>>.Failure("ESI_403", "required role(s)")));

        EsiCorporationCapabilityResolver sut = new(esiClient);

        var result = await sut.ResolveAsync(
            corporationId,
            characterId,
            ["esi-assets.read_corporation_assets", "esi-corporations.read_corporation_membership"]);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasDirectorRole.Should().BeFalse();
        result.Value.HasAssetAccess.Should().BeFalse();
        result.Value.HasAnyAccess.Should().BeFalse();
    }
}