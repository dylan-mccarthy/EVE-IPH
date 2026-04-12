using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Models;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.UI.Avalonia.Services;
using EVE.IPH.UI.Avalonia.ViewModels;
using NSubstitute;

namespace EVE.IPH.UI.Avalonia.Tests.ViewModels;

public sealed class StructureFacilityManagementViewModelTests
{
    [Fact]
    public async Task LoadTask_LoadsCharactersStructuresAndFacilities()
    {
        IStructureFacilityManagementQueryService queryService = Substitute.For<IStructureFacilityManagementQueryService>();
        IStructureFacilityManagementCommandService commandService = Substitute.For<IStructureFacilityManagementCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(BuildScreenData());

        StructureFacilityManagementViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        viewModel.Characters.Should().ContainSingle();
        viewModel.Structures.Should().ContainSingle();
        viewModel.FacilityItems.Should().ContainSingle();
        viewModel.SelectedCharacter.Should().NotBeNull();
        viewModel.SelectedProductionType.Should().NotBeNull();
    }

    [Fact]
    public async Task UseSelectedStructureForFacility_CopiesSelectedStructureIntoFacilityForm()
    {
        IStructureFacilityManagementQueryService queryService = Substitute.For<IStructureFacilityManagementQueryService>();
        IStructureFacilityManagementCommandService commandService = Substitute.For<IStructureFacilityManagementCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(BuildScreenData());

        StructureFacilityManagementViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        viewModel.UseSelectedStructureForFacility();

        viewModel.FacilityId.Should().Be(4001);
        viewModel.FacilityName.Should().Be("Tatara Alpha");
        viewModel.SelectedFacilityKind!.FacilityKind.Should().Be(IndustryFacilityKind.UpwellStructure);
    }

    [Fact]
    public async Task SaveFacilityAsync_ReloadsAndUpdatesStatus()
    {
        IStructureFacilityManagementQueryService queryService = Substitute.For<IStructureFacilityManagementQueryService>();
        IStructureFacilityManagementCommandService commandService = Substitute.For<IStructureFacilityManagementCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(BuildScreenData(), BuildScreenData());
        commandService.SaveFacilityAsync(Arg.Any<FacilitySettingsUpsertRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<IndustryFacilityConfigurationRecord>.Success(new IndustryFacilityConfigurationRecord(new CharacterId(1001), FacilityProductionType.Manufacturing, 4001, "Tatara Alpha", IndustryFacilityKind.UpwellStructure, 10000002, "The Forge", 30000142, "Jita", 0.9, 0.035, 0, true, true, true, false, 0, 0.01, Maybe<double>.None, Maybe<double>.None, Maybe<double>.None)));

        StructureFacilityManagementViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;
        viewModel.ModuleTypeIdsText = "6001,6002";

        await viewModel.SaveFacilityAsync();

        viewModel.StatusText.Should().Contain("Saved Manufacturing settings");
        int[] expectedModuleIds = [6001, 6002];
        await commandService.Received(1).SaveFacilityAsync(Arg.Is<FacilitySettingsUpsertRequest>(request => request.ModuleTypeIds.SequenceEqual(expectedModuleIds)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteSelectedStructureAsync_ReloadsAfterDelete()
    {
        IStructureFacilityManagementQueryService queryService = Substitute.For<IStructureFacilityManagementQueryService>();
        IStructureFacilityManagementCommandService commandService = Substitute.For<IStructureFacilityManagementCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(BuildScreenData(), BuildScreenData() with { Structures = [] });
        commandService.DeleteStructureAsync(4001, Arg.Any<CancellationToken>()).Returns(Result<bool>.Success(true));

        StructureFacilityManagementViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        await viewModel.DeleteSelectedStructureAsync();

        viewModel.Structures.Should().BeEmpty();
        viewModel.StatusText.Should().Contain("Deleted structure");
    }

    private static StructureFacilityManagementScreenData BuildScreenData() => new(
        [new StructureFacilityCharacterOption(new CharacterId(1001), "Kara Maken", true)],
        [new IndustryStructureRow(4001, "Tatara Alpha", 35835, 30000142, 10000002, 2001, true, DateTimeOffset.UtcNow)],
        [new FacilitySettingsRow(new CharacterId(1001), "Kara Maken", FacilityProductionType.Manufacturing, 4001, "Tatara Alpha", IndustryFacilityKind.UpwellStructure, 4001, "Tatara Alpha", 10000002, "The Forge", 30000142, "Jita", 0.9, 0.035, 0, true, true, true, false, 0, 0.01, null, null, null, [6001, 6002])],
        [new FacilityProductionTypeOption(FacilityProductionType.Manufacturing, "Manufacturing")],
        [new FacilityKindOption(IndustryFacilityKind.UpwellStructure, "Upwell Structure")],
        "Loaded structures and facilities.");
}