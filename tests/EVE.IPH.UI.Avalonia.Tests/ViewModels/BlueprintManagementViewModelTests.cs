using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.UI.Avalonia.Services;
using EVE.IPH.UI.Avalonia.ViewModels;
using NSubstitute;

namespace EVE.IPH.UI.Avalonia.Tests.ViewModels;

public sealed class BlueprintManagementViewModelTests
{
    [Fact]
    public async Task Constructor_LoadsBlueprintsAndOwnerOptions()
    {
        IBlueprintManagementQueryService queryService = Substitute.For<IBlueprintManagementQueryService>();
        IBlueprintManagementCommandService commandService = Substitute.For<IBlueprintManagementCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(CreateScreenData([
            CreateRow(90000001, "Kara Maken", false, 28607, "Character Blueprint"),
            CreateRow(98000001, "Acme Holdings", true, 28608, "Corporation Blueprint"),
        ]));

        BlueprintManagementViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        viewModel.Items.Should().HaveCount(2);
        viewModel.OwnerOptions.Select(option => option.DisplayName).Should().Contain("Acme Holdings");
        viewModel.SelectedBlueprint.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveSelectedBlueprintAsync_UsesEditedValuesAndReloads()
    {
        IBlueprintManagementQueryService queryService = Substitute.For<IBlueprintManagementQueryService>();
        IBlueprintManagementCommandService commandService = Substitute.For<IBlueprintManagementCommandService>();

        BlueprintManagementRow initialRow = CreateRow(90000001, "Kara Maken", false, 28607, "Character Blueprint");
        BlueprintManagementRow updatedRow = initialRow with { BlueprintName = "Edited Blueprint", Me = 7, Te = 14, Runs = 5 };

        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(
            CreateScreenData([initialRow]),
            CreateScreenData([updatedRow]));
        commandService.SaveBlueprintAsync(Arg.Any<OwnedBlueprintRecord>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<OwnedBlueprintRecord>.Success(call.Arg<OwnedBlueprintRecord>()));

        BlueprintManagementViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;
        viewModel.SelectedBlueprint = viewModel.Items.Single();
        viewModel.EditBlueprintName = "Edited Blueprint";
        viewModel.EditMe = 7;
        viewModel.EditTe = 14;
        viewModel.EditRuns = 5;

        await viewModel.SaveSelectedBlueprintAsync();

        await commandService.Received(1).SaveBlueprintAsync(
            Arg.Is<OwnedBlueprintRecord>(record => record.BlueprintName == "Edited Blueprint" && record.Me == 7 && record.Te == 14 && record.Runs == 5),
            Arg.Any<CancellationToken>());
        viewModel.Items.Single().BlueprintName.Should().Be("Edited Blueprint");
        viewModel.StatusText.Should().Contain("Saved blueprint");
    }

    [Fact]
    public async Task DeleteSelectedBlueprintAsync_RemovesRowAfterReload()
    {
        IBlueprintManagementQueryService queryService = Substitute.For<IBlueprintManagementQueryService>();
        IBlueprintManagementCommandService commandService = Substitute.For<IBlueprintManagementCommandService>();

        BlueprintManagementRow row = CreateRow(90000001, "Kara Maken", false, 28607, "Character Blueprint");
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(
            CreateScreenData([row]),
            CreateScreenData([]));
        commandService.DeleteBlueprintAsync(row.OwnerId, row.BlueprintId, Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Success(true));

        BlueprintManagementViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;
        viewModel.SelectedBlueprint = viewModel.Items.Single();

        await viewModel.DeleteSelectedBlueprintAsync();

        viewModel.Items.Should().BeEmpty();
        viewModel.StatusText.Should().Contain("Deleted blueprint");
    }

    [Fact]
    public async Task RefreshAsync_UsesCommandServiceAndReloadsRows()
    {
        IBlueprintManagementQueryService queryService = Substitute.For<IBlueprintManagementQueryService>();
        IBlueprintManagementCommandService commandService = Substitute.For<IBlueprintManagementCommandService>();

        BlueprintManagementRow initialRow = CreateRow(90000001, "Kara Maken", false, 28607, "Character Blueprint");
        BlueprintManagementRow refreshedRow = initialRow with { BlueprintName = "Refreshed Blueprint" };

        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(CreateScreenData([initialRow]));
        commandService.RefreshAsync(Arg.Any<CancellationToken>()).Returns(CreateScreenData([refreshedRow]) with { StatusText = "Refreshed connected owner data for 1 character and reloaded owned blueprints." });

        BlueprintManagementViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        await viewModel.RefreshAsync();

        viewModel.Items.Single().BlueprintName.Should().Be("Refreshed Blueprint");
        viewModel.StatusText.Should().Contain("reloaded owned blueprints");
    }

    private static BlueprintManagementScreenData CreateScreenData(IReadOnlyList<BlueprintManagementRow> rows) =>
        new(
            rows,
            new[] { new BlueprintOwnerFilterOption(null, "All Owners") }
                .Concat(rows
                    .Select(row => new BlueprintOwnerFilterOption(row.OwnerId, row.OwnerName))
                    .DistinctBy(option => option.OwnerId)
                    .OrderBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToArray())
                .ToArray(),
            "Loaded owned blueprints for the connected characters and corporations.");

    private static BlueprintManagementRow CreateRow(long ownerId, string ownerName, bool isCorporationOwner, long blueprintId, string blueprintName) =>
        new(ownerId, ownerName, isCorporationOwner, new ItemId(7000000 + blueprintId), 60015068, new BlueprintId(blueprintId), blueprintName, 1, 10, 20, -1, 1, true, true);
}