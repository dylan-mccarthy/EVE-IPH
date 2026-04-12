using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Models;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Manufacturing.Services;
using EVE.IPH.UI.Avalonia.Services;
using NSubstitute;

namespace EVE.IPH.UI.Avalonia.Tests.Services;

public sealed class StructureFacilityManagementServiceTests
{
    [Fact]
    public async Task QueryService_LoadsStructuresCharactersAndFacilities()
    {
        ICharacterManagementQueryService characterQuery = Substitute.For<ICharacterManagementQueryService>();
        IIndustryFacilityRepository repository = Substitute.For<IIndustryFacilityRepository>();
        IManufacturingFacilityConfigurationService facilityConfigurationService = Substitute.For<IManufacturingFacilityConfigurationService>();

        repository.GetStructuresAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<IndustryStructureRecord>>.Success(
        [
            new IndustryStructureRecord(4001, "Tatara Alpha", 35835, 30000142, 10000002, Maybe<long>.Some(2001), true, DateTimeOffset.UtcNow),
        ]));

        characterQuery.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(Result<CharacterManagementScreenData>.Success(new CharacterManagementScreenData(
            [new CharacterManagementCharacterRow(new CharacterRecord(new CharacterId(1001), "Kara Maken", new CorporationId(2001), Maybe<AllianceId>.None, true), new CharacterTokenStatus(new CharacterId(1001), true, false, null, "Healthy", []))],
            [],
            "Loaded")));

        facilityConfigurationService.GetFacilitiesAsync(new CharacterId(1001), Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<ResolvedIndustryFacilityConfiguration>>.Success(
        [
            new ResolvedIndustryFacilityConfiguration(
                new IndustryFacilityConfigurationRecord(new CharacterId(1001), FacilityProductionType.Manufacturing, 4001, "Tatara Alpha", IndustryFacilityKind.UpwellStructure, 10000002, "The Forge", 30000142, "Jita", 0.9, 0.035, 0, true, true, true, false, 0, 0.01, Maybe<double>.None, Maybe<double>.None, Maybe<double>.None),
                Maybe<IndustryStructureRecord>.Some(new IndustryStructureRecord(4001, "Tatara Alpha", 35835, 30000142, 10000002, Maybe<long>.Some(2001), true, DateTimeOffset.UtcNow)),
                [new IndustryFacilityModuleRecord(new CharacterId(1001), FacilityProductionType.Manufacturing, 4001, 6001)]),
        ]));

        StructureFacilityManagementQueryService sut = new(characterQuery, repository, facilityConfigurationService);

        StructureFacilityManagementScreenData result = await sut.GetScreenDataAsync();

        result.Characters.Should().ContainSingle();
        result.Structures.Should().ContainSingle();
        result.Facilities.Should().ContainSingle();
        result.Facilities[0].InstalledModuleTypeIds.Should().Contain(6001);
    }

    [Fact]
    public async Task CommandService_SaveFacilityAsync_UpsertsConfigurationAndModules()
    {
        IIndustryFacilityRepository repository = Substitute.For<IIndustryFacilityRepository>();
        ICharacterManagementQueryService characterQuery = Substitute.For<ICharacterManagementQueryService>();

        repository.UpsertFacilityAsync(Arg.Any<IndustryFacilityConfigurationRecord>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<IndustryFacilityConfigurationRecord>.Success(call.Arg<IndustryFacilityConfigurationRecord>()));
        repository.ReplaceInstalledModulesAsync(new CharacterId(1001), FacilityProductionType.Manufacturing, 4001, Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<IndustryFacilityModuleRecord>>.Success([]));

        StructureFacilityManagementCommandService sut = new(repository, characterQuery);

        Result<IndustryFacilityConfigurationRecord> result = await sut.SaveFacilityAsync(new FacilitySettingsUpsertRequest(
            new CharacterId(1001),
            FacilityProductionType.Manufacturing,
            4001,
            "Tatara Alpha",
            IndustryFacilityKind.UpwellStructure,
            10000002,
            "The Forge",
            30000142,
            "Jita",
            0.9,
            0.035,
            0,
            true,
            true,
            true,
            false,
            0,
            0.01,
            null,
            null,
            null,
            [6001, 6002]));

        result.IsSuccess.Should().BeTrue();
    int[] expectedModuleIds = [6001, 6002];
    await repository.Received(1).ReplaceInstalledModulesAsync(new CharacterId(1001), FacilityProductionType.Manufacturing, 4001, Arg.Is<IReadOnlyList<int>>(ids => ids.SequenceEqual(expectedModuleIds)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CommandService_DeleteStructureAsync_CleansMatchingFacilitiesFirst()
    {
        IIndustryFacilityRepository repository = Substitute.For<IIndustryFacilityRepository>();
        ICharacterManagementQueryService characterQuery = Substitute.For<ICharacterManagementQueryService>();

        characterQuery.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(Result<CharacterManagementScreenData>.Success(new CharacterManagementScreenData(
            [new CharacterManagementCharacterRow(new CharacterRecord(new CharacterId(1001), "Kara Maken", new CorporationId(2001), Maybe<AllianceId>.None, true), new CharacterTokenStatus(new CharacterId(1001), true, false, null, "Healthy", []))],
            [],
            "Loaded")));
        repository.GetFacilitiesAsync(new CharacterId(1001), Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<IndustryFacilityConfigurationRecord>>.Success(
        [
            new IndustryFacilityConfigurationRecord(new CharacterId(1001), FacilityProductionType.Manufacturing, 4001, "Tatara Alpha", IndustryFacilityKind.UpwellStructure, 10000002, "The Forge", 30000142, "Jita", 0.9, 0.035, 0, true, true, true, false, 0, 0.01, Maybe<double>.None, Maybe<double>.None, Maybe<double>.None),
        ]));
        repository.ReplaceInstalledModulesAsync(new CharacterId(1001), FacilityProductionType.Manufacturing, 4001, [], Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<IndustryFacilityModuleRecord>>.Success([]));
        repository.DeleteFacilityAsync(new CharacterId(1001), FacilityProductionType.Manufacturing, Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Success(true));
        repository.DeleteStructureAsync(4001, Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Success(true));

        StructureFacilityManagementCommandService sut = new(repository, characterQuery);

        Result<bool> result = await sut.DeleteStructureAsync(4001);

        result.IsSuccess.Should().BeTrue();
        await repository.Received(1).DeleteFacilityAsync(new CharacterId(1001), FacilityProductionType.Manufacturing, Arg.Any<CancellationToken>());
        await repository.Received(1).DeleteStructureAsync(4001, Arg.Any<CancellationToken>());
    }
}