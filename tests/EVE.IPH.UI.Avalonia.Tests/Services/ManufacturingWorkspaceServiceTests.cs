using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Models;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Manufacturing.Models;
using EVE.IPH.Domain.Manufacturing.Services;
using EVE.IPH.Infrastructure.Settings.Models;
using EVE.IPH.UI.Avalonia.Services;
using NSubstitute;

namespace EVE.IPH.UI.Avalonia.Tests.Services;

public sealed class ManufacturingWorkspaceServiceTests
{
    [Fact]
    public async Task QueryService_LoadsMixedOwnerBlueprintsAndManufacturingFacilities()
    {
        ICharacterManagementQueryService characterQuery = Substitute.For<ICharacterManagementQueryService>();
        IOwnedBlueprintWorkflowService workflow = Substitute.For<IOwnedBlueprintWorkflowService>();
        IManufacturingFacilityConfigurationService facilityService = Substitute.For<IManufacturingFacilityConfigurationService>();

        characterQuery.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(Result<CharacterManagementScreenData>.Success(new CharacterManagementScreenData(
            [new CharacterManagementCharacterRow(new CharacterRecord(new CharacterId(1001), "Kara Maken", new CorporationId(2001), Maybe<AllianceId>.None, true), new CharacterTokenStatus(new CharacterId(1001), true, false, null, "ok", []))],
            [new CharacterManagementCorporationRow(new CorporationConnectionRecord(new CorporationId(2001), "Signal Cartel", new CharacterId(1001), true, true, true), "Kara Maken")],
            "Loaded")));

        workflow.GetBlueprintsByOwnersAsync(Arg.Any<IReadOnlyList<long>>(), Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<OwnedBlueprintViewRecord>>.Success(
        [
            new OwnedBlueprintViewRecord(1001, "Kara Maken", false, 5001, 6001, 28607, "Vargur Blueprint", 1, 10, 20, -1, 1, true, true),
            new OwnedBlueprintViewRecord(2001, "Signal Cartel", true, 5002, 6002, 2046, "Hobgoblin Blueprint", 2, 8, 14, -1, 1, true, true),
        ]));

        facilityService.GetFacilityAsync(new CharacterId(1001), FacilityProductionType.Manufacturing, Arg.Any<CancellationToken>()).Returns(
            Result<Maybe<ResolvedIndustryFacilityConfiguration>>.Success(Maybe<ResolvedIndustryFacilityConfiguration>.Some(new ResolvedIndustryFacilityConfiguration(
                new IndustryFacilityConfigurationRecord(new CharacterId(1001), FacilityProductionType.Manufacturing, 4001, "Tatara Alpha", IndustryFacilityKind.UpwellStructure, 10000002, "The Forge", 30000142, "Jita", 0.9, 0.035, 0, true, true, true, false, 0, 0.01, Maybe<double>.None, Maybe<double>.None, Maybe<double>.None),
                Maybe<IndustryStructureRecord>.None,
                []))));

        ManufacturingWorkspaceQueryService sut = new(characterQuery, workflow, facilityService);

        ManufacturingWorkspaceScreenData result = await sut.GetScreenDataAsync();

        result.Blueprints.Should().HaveCount(2);
        result.Facilities.Should().ContainSingle();
        result.StatusText.Should().Contain("Loaded");
    }

    [Fact]
    public async Task CommandService_AnalyzeAsync_ComposesManufacturingSnapshot()
    {
        IBlueprintRepository blueprintRepository = Substitute.For<IBlueprintRepository>();
        IItemRepository itemRepository = Substitute.For<IItemRepository>();
        ICharacterSkillRepository skillRepository = Substitute.For<ICharacterSkillRepository>();
        IManufacturingFacilityConfigurationService facilityService = Substitute.For<IManufacturingFacilityConfigurationService>();

        blueprintRepository.GetBlueprintAsync(new BlueprintId(28607), Arg.Any<CancellationToken>())
            .Returns(Maybe<BlueprintRecord>.Some(new BlueprintRecord(new BlueprintId(28607), new TypeId(22436), "Vargur", TechLevel.T1, 1, 28800, 0, 0, 0, 0)));
        blueprintRepository.GetRequiredSkillsAsync(new BlueprintId(28607), ActivityType.Manufacturing, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<SkillRequirement>>.Success([new SkillRequirement(new TypeId(3398), 4)]));
        itemRepository.GetItemAsync(new TypeId(22436), Arg.Any<CancellationToken>())
            .Returns(Maybe<ItemRecord>.Some(new ItemRecord(new TypeId(22436), "Vargur", 1, "Ships", 1, 5000, 1)));
        skillRepository.GetByCharacterIdAsync(new CharacterId(1001), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CharacterSkillRecord>>.Success(
            [
                new CharacterSkillRecord(new CharacterId(1001), new TypeId(3398), "Minmatar Battleship", 5, 5, 0, false, 0),
                new CharacterSkillRecord(new CharacterId(1001), new TypeId(11442), "Advanced Industry", 5, 5, 0, false, 0),
                new CharacterSkillRecord(new CharacterId(1001), new TypeId(3402), "Science", 5, 5, 0, false, 0),
                new CharacterSkillRecord(new CharacterId(1001), new TypeId(16622), "Accounting", 5, 5, 0, false, 0),
                new CharacterSkillRecord(new CharacterId(1001), new TypeId(3446), "Broker Relations", 5, 5, 0, false, 0),
            ]));
        facilityService.GetFacilityAsync(new CharacterId(1001), FacilityProductionType.Manufacturing, Arg.Any<CancellationToken>())
            .Returns(Result<Maybe<ResolvedIndustryFacilityConfiguration>>.Success(Maybe<ResolvedIndustryFacilityConfiguration>.Some(new ResolvedIndustryFacilityConfiguration(
                new IndustryFacilityConfigurationRecord(new CharacterId(1001), FacilityProductionType.Manufacturing, 4001, "Tatara Alpha", IndustryFacilityKind.UpwellStructure, 10000002, "The Forge", 30000142, "Jita", 0.9, 0.01, 0, true, true, true, false, 0, 0.005, Maybe<double>.None, Maybe<double>.None, Maybe<double>.None),
                Maybe<IndustryStructureRecord>.None,
                []))));

        ManufacturingWorkspaceCommandService sut = new(
            blueprintRepository,
            itemRepository,
            skillRepository,
            facilityService,
            new ManufacturingAnalysisService(
                new ManufacturingPrerequisiteService(),
                new ManufacturingFacilityUsageCalculator(),
                new ManufacturingUsageAllocationCalculator(),
                new ManufacturingSaleAdjustmentCalculator(),
                new ManufacturingBuildBuyDecider(),
                new ManufacturingActivityCalculator(),
                new ComponentProductionScheduleCalculator(),
                new ManufacturingCostCalculator(),
                new ManufacturingTimelineCalculator(),
                new ManufacturingProfitabilityCalculator()),
            new ApplicationSettingsModel());

        Result<ManufacturingWorkspaceAnalysisResult> result = await sut.AnalyzeAsync(new ManufacturingWorkspaceAnalysisRequest(
            new BlueprintId(28607),
            new CharacterId(1001),
            FacilityProductionType.Manufacturing,
            4001,
            2,
            500_000_000,
            320_000_000,
            0,
            25_000_000,
            400_000_000,
            true,
            true));

        result.IsSuccess.Should().BeTrue();
        result.Value.ProductName.Should().Be("Vargur");
        result.Value.FacilityName.Should().Be("Tatara Alpha");
        result.Value.TotalProductionTimeSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CommandService_AnalyzeAsync_WhenFacilityMissing_ReturnsFailure()
    {
        IBlueprintRepository blueprintRepository = Substitute.For<IBlueprintRepository>();
        IItemRepository itemRepository = Substitute.For<IItemRepository>();
        ICharacterSkillRepository skillRepository = Substitute.For<ICharacterSkillRepository>();
        IManufacturingFacilityConfigurationService facilityService = Substitute.For<IManufacturingFacilityConfigurationService>();

        blueprintRepository.GetBlueprintAsync(new BlueprintId(28607), Arg.Any<CancellationToken>())
            .Returns(Maybe<BlueprintRecord>.Some(new BlueprintRecord(new BlueprintId(28607), new TypeId(22436), "Vargur", TechLevel.T1, 1, 28800, 0, 0, 0, 0)));
        blueprintRepository.GetRequiredSkillsAsync(new BlueprintId(28607), ActivityType.Manufacturing, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<SkillRequirement>>.Success([]));
        itemRepository.GetItemAsync(new TypeId(22436), Arg.Any<CancellationToken>())
            .Returns(Maybe<ItemRecord>.Some(new ItemRecord(new TypeId(22436), "Vargur", 1, "Ships", 1, 5000, 1)));
        skillRepository.GetByCharacterIdAsync(new CharacterId(1001), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CharacterSkillRecord>>.Success([]));
        facilityService.GetFacilityAsync(new CharacterId(1001), FacilityProductionType.Manufacturing, Arg.Any<CancellationToken>())
            .Returns(Result<Maybe<ResolvedIndustryFacilityConfiguration>>.Success(Maybe<ResolvedIndustryFacilityConfiguration>.None));

        ManufacturingWorkspaceCommandService sut = new(
            blueprintRepository,
            itemRepository,
            skillRepository,
            facilityService,
            new ManufacturingAnalysisService(
                new ManufacturingPrerequisiteService(),
                new ManufacturingFacilityUsageCalculator(),
                new ManufacturingUsageAllocationCalculator(),
                new ManufacturingSaleAdjustmentCalculator(),
                new ManufacturingBuildBuyDecider(),
                new ManufacturingActivityCalculator(),
                new ComponentProductionScheduleCalculator(),
                new ManufacturingCostCalculator(),
                new ManufacturingTimelineCalculator(),
                new ManufacturingProfitabilityCalculator()));

        Result<ManufacturingWorkspaceAnalysisResult> result = await sut.AnalyzeAsync(new ManufacturingWorkspaceAnalysisRequest(
            new BlueprintId(28607),
            new CharacterId(1001),
            FacilityProductionType.Manufacturing,
            4001,
            1,
            100,
            50,
            0,
            0,
            100,
            true,
            true));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FACILITY_NOT_FOUND");
    }
}