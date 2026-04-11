using EVE.IPH.Domain.Assets.Models;
using EVE.IPH.Domain.Assets.Services;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.UI.Avalonia.Services;
using EVE.IPH.UI.Avalonia.ViewModels;
using NSubstitute;

namespace EVE.IPH.UI.Avalonia.Tests.ViewModels;

public sealed class AssetsViewModelTests
{
    [Fact]
    public async Task Constructor_LoadsAllAssetsAndOwnerFilters()
    {
        AssetsViewModel viewModel = CreateViewModel();
        await viewModel.LoadTask;

        viewModel.Items.Should().HaveCount(3);
        viewModel.OwnerOptions.Should().HaveCount(3);
        viewModel.OwnerOptions[0].DisplayName.Should().Be("All Owners");
        viewModel.StatusText.Should().Be("Loaded synced asset records from the local SQLite store. Showing 3 of 3 hydrated assets.");
    }

    [Fact]
    public async Task SearchText_WhenUpdated_FiltersVisibleAssets()
    {
        AssetsViewModel viewModel = CreateViewModel();
        await viewModel.LoadTask;

        viewModel.SearchText = "Heavy Water";

        viewModel.Items.Should().ContainSingle();
        viewModel.Items[0].TypeName.Should().Be("Heavy Water");
        viewModel.StatusText.Should().Be("Loaded synced asset records from the local SQLite store. Showing 1 of 3 hydrated assets.");
    }

    [Fact]
    public async Task OnlyBlueprintCopies_WithSearchText_ExcludesBlueprintOriginals()
    {
        AssetsViewModel viewModel = CreateViewModel();
        await viewModel.LoadTask;
        viewModel.SearchText = "Vargur";

        viewModel.OnlyBlueprintCopies = true;

        viewModel.Items.Should().ContainSingle();
        viewModel.Items[0].BlueprintKind.Should().Be(AssetBlueprintKind.Copy);
    }

    [Fact]
    public async Task SelectedOwner_WhenUpdated_FiltersVisibleAssets()
    {
        AssetsViewModel viewModel = CreateViewModel();
        await viewModel.LoadTask;

        viewModel.SelectedOwner = viewModel.OwnerOptions.Single(option => option.OwnerId == 1002);

        viewModel.Items.Should().HaveCount(1);
        viewModel.Items[0].OwnerId.Should().Be(1002);
        viewModel.StatusText.Should().Be("Loaded synced asset records from the local SQLite store. Showing 1 of 3 hydrated assets for Mina Kall.");
    }

    [Fact]
    public async Task RefreshAsync_WhenCalled_ReloadsAssets()
    {
        IAssetsScreenService screenService = Substitute.For<IAssetsScreenService>();
        screenService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(CreateScreenData([
            CreateHydratedAsset(1001, 1, AssetBlueprintKind.None, "Heavy Water"),
        ])));
        screenService.RefreshAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new AssetsScreenData(
            [CreateHydratedAsset(1002, 2, AssetBlueprintKind.Copy, "Vargur Blueprint")],
            [new AssetOwnerFilterOption(null, "All Owners"), new AssetOwnerFilterOption(1002, "Mina Kall")],
            "Refreshed assets for 1 connected character.")));

        AssetsViewModel viewModel = new(new AssetViewFilterService(), screenService);
        await viewModel.LoadTask;

        await viewModel.RefreshAsync();

        viewModel.Items.Should().ContainSingle();
        viewModel.Items[0].OwnerId.Should().Be(1002);
        viewModel.StatusText.Should().Be("Refreshed assets for 1 connected character. Showing 1 of 1 hydrated assets.");
    }

    private static AssetsViewModel CreateViewModel()
    {
        IAssetsScreenService screenService = Substitute.For<IAssetsScreenService>();
        screenService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(CreateScreenData([
            CreateHydratedAsset(1001, 1, AssetBlueprintKind.None, "Heavy Water"),
            CreateHydratedAsset(1001, 2, AssetBlueprintKind.Original, "Vargur Blueprint"),
            CreateHydratedAsset(1002, 3, AssetBlueprintKind.Copy, "Vargur Blueprint"),
        ])));
        screenService.RefreshAsync(Arg.Any<CancellationToken>()).Returns(call => screenService.GetScreenDataAsync(call.Arg<CancellationToken>()));

        return new AssetsViewModel(new AssetViewFilterService(), screenService);
    }

    private static AssetsScreenData CreateScreenData(IReadOnlyList<HydratedAsset> assets) => new(
        assets,
        [new AssetOwnerFilterOption(null, "All Owners"), new AssetOwnerFilterOption(1001, "Kara Maken"), new AssetOwnerFilterOption(1002, "Mina Kall")],
        "Loaded synced asset records from the local SQLite store.");

    private static HydratedAsset CreateHydratedAsset(long ownerId, long itemId, AssetBlueprintKind blueprintKind, string typeName) =>
        new(
            OwnerId: ownerId,
            ItemId: itemId,
            LocationId: 6000001,
            TypeId: new TypeId(itemId + 100),
            Quantity: 1,
            FlagId: 4,
            IsSingleton: true,
            BlueprintKind: blueprintKind,
            ItemName: string.Empty,
            TypeName: typeName,
            TypeGroup: blueprintKind == AssetBlueprintKind.None ? "Materials" : "Blueprints",
            TypeCategory: blueprintKind == AssetBlueprintKind.None ? "Material" : "Blueprint",
            LocationName: "Jita 4-4",
            FlagText: "Item Hangar",
            Container: false,
            FlagSort: 1);
}