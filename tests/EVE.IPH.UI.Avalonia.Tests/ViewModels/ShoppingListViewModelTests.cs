using EVE.IPH.Domain.Core.Results;
using EVE.IPH.UI.Avalonia.Services;
using EVE.IPH.UI.Avalonia.ViewModels;
using NSubstitute;

namespace EVE.IPH.UI.Avalonia.Tests.ViewModels;

public sealed class ShoppingListViewModelTests
{
    [Fact]
    public async Task LoadTask_LoadsPersistedShoppingList()
    {
        IShoppingListWorkspaceQueryService queryService = Substitute.For<IShoppingListWorkspaceQueryService>();
        IShoppingListWorkspaceCommandService commandService = Substitute.For<IShoppingListWorkspaceCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(CreateScreenData());

        ShoppingListViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        viewModel.Items.Should().ContainSingle();
        viewModel.ItemCount.Should().Be(1);
        viewModel.TotalCost.Should().Be(500d);
    }

    [Fact]
    public async Task ClearAsync_WhenSuccessful_UpdatesScreenData()
    {
        IShoppingListWorkspaceQueryService queryService = Substitute.For<IShoppingListWorkspaceQueryService>();
        IShoppingListWorkspaceCommandService commandService = Substitute.For<IShoppingListWorkspaceCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(CreateScreenData());
        commandService.ClearAsync(Arg.Any<CancellationToken>()).Returns(Result<ShoppingListScreenData>.Success(new ShoppingListScreenData([], 0, 0, 0d, "No persisted shopping-list rows were found yet.")));

        ShoppingListViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        await viewModel.ClearAsync();

        viewModel.Items.Should().BeEmpty();
        viewModel.StatusText.Should().Contain("No persisted shopping-list rows");
    }

    [Fact]
    public async Task RemoveItemAsync_WhenCommandFails_ExposesFailureStatus()
    {
        IShoppingListWorkspaceQueryService queryService = Substitute.For<IShoppingListWorkspaceQueryService>();
        IShoppingListWorkspaceCommandService commandService = Substitute.For<IShoppingListWorkspaceCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(CreateScreenData());
        commandService.RemoveItemAsync(34, Arg.Any<CancellationToken>()).Returns(Result<ShoppingListScreenData>.Failure("DB_ERROR", "delete failed"));

        ShoppingListViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        await viewModel.RemoveItemAsync(34);

        viewModel.StatusText.Should().Contain("Unable to remove the shopping-list row").And.Contain("delete failed");
        viewModel.Items.Should().ContainSingle();
    }

    private static ShoppingListScreenData CreateScreenData() => new(
        [new ShoppingListRow(34, "Tritanium", 100, 5d)],
        1,
        100,
        500d,
        "Loaded 1 persisted shopping-list row from the local SQLite store.");
}