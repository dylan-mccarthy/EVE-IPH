using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.ShoppingList.Models;
using EVE.IPH.Domain.ShoppingList.Services;
using EVE.IPH.UI.Avalonia.Services;
using NSubstitute;

namespace EVE.IPH.UI.Avalonia.Tests.Services;

public sealed class ShoppingListWorkspaceServiceTests
{
    [Fact]
    public async Task QueryService_GetScreenDataAsync_LoadsPersistedRows()
    {
        IShoppingListService shoppingListService = Substitute.For<IShoppingListService>();
        AggregatedShoppingList list = new(
            [new ShoppingListLineItem(new TypeId(34), "Tritanium", "Buy", 100, 0d, 5d, Kind: ShoppingListLineItemKind.Buy)],
            500d,
            0d);
        shoppingListService.GetPersistedAsync(Arg.Any<CancellationToken>()).Returns(Result<AggregatedShoppingList>.Success(list));
        shoppingListService.Project(list, Arg.Any<IReadOnlyCollection<ShoppingListLineItem>>(), Arg.Any<IReadOnlyCollection<ShoppingListLineItem>>())
            .Returns(Result<ShoppingListProjection>.Success(new ShoppingListProjection(list, list, new([], 0d, 0d), new([], 0d, 0d), new([], 0d, 0d), new([], 0d, 0d), list, new([], 0d, 0d))));

        ShoppingListWorkspaceQueryService service = new(shoppingListService);

        ShoppingListScreenData result = await service.GetScreenDataAsync();

        result.Items.Should().ContainSingle();
        result.ItemCount.Should().Be(1);
        result.TotalQuantity.Should().Be(100);
        result.TotalCost.Should().Be(500d);
    }

    [Fact]
    public async Task CommandService_ClearAsync_ClearsRepositoryAndReloads()
    {
        IShoppingListRepository repository = Substitute.For<IShoppingListRepository>();
        IShoppingListWorkspaceQueryService queryService = Substitute.For<IShoppingListWorkspaceQueryService>();
        repository.ClearAsync(Arg.Any<CancellationToken>()).Returns(Result<bool>.Success(true));
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(new ShoppingListScreenData([], 0, 0, 0d, "No persisted shopping-list rows were found yet."));

        ShoppingListWorkspaceCommandService service = new(repository, queryService);

        Result<ShoppingListScreenData> result = await service.ClearAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        await repository.Received(1).ClearAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CommandService_RemoveItemAsync_WhenTypeIdInvalid_ReturnsFailure()
    {
        ShoppingListWorkspaceCommandService service = new(Substitute.For<IShoppingListRepository>(), Substitute.For<IShoppingListWorkspaceQueryService>());

        Result<ShoppingListScreenData> result = await service.RemoveItemAsync(0);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_TYPE_ID");
    }
}