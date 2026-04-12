using CommunityToolkit.Mvvm.ComponentModel;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.UI.Avalonia.Services;

namespace EVE.IPH.UI.Avalonia.ViewModels;

public sealed class ShoppingListViewModel : ObservableObject
{
    private readonly IShoppingListWorkspaceQueryService _queryService;
    private readonly IShoppingListWorkspaceCommandService _commandService;
    private IReadOnlyList<ShoppingListRow> _items = [];
    private string _statusText = "Loading shopping list...";
    private bool _isBusy;
    private int _itemCount;
    private long _totalQuantity;
    private double _totalCost;

    public ShoppingListViewModel(
        IShoppingListWorkspaceQueryService queryService,
        IShoppingListWorkspaceCommandService commandService)
    {
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

        LoadTask = LoadAsync();
    }

    public Task LoadTask { get; }

    public IReadOnlyList<ShoppingListRow> Items
    {
        get => _items;
        private set => SetProperty(ref _items, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanRefresh));
                OnPropertyChanged(nameof(CanClearItems));
            }
        }
    }

    public int ItemCount
    {
        get => _itemCount;
        private set => SetProperty(ref _itemCount, value);
    }

    public long TotalQuantity
    {
        get => _totalQuantity;
        private set => SetProperty(ref _totalQuantity, value);
    }

    public double TotalCost
    {
        get => _totalCost;
        private set => SetProperty(ref _totalCost, value);
    }

    public bool CanRefresh => !IsBusy;

    public bool CanClearItems => !IsBusy && Items.Count > 0;

    public async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        await LoadAsync().ConfigureAwait(false);
    }

    public async Task ClearAsync()
    {
        if (!CanClearItems)
        {
            return;
        }

        try
        {
            IsBusy = true;
            Result<ShoppingListScreenData> result = await _commandService.ClearAsync().ConfigureAwait(false);
            if (result.IsFailure)
            {
                StatusText = $"Unable to clear the shopping list: {result.Error.Message}";
                return;
            }

            ApplyScreenData(result.Value);
        }
        catch (Exception ex)
        {
            StatusText = $"Unable to clear the shopping list: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RemoveItemAsync(long typeId)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            Result<ShoppingListScreenData> result = await _commandService.RemoveItemAsync(typeId).ConfigureAwait(false);
            if (result.IsFailure)
            {
                StatusText = $"Unable to remove the shopping-list row: {result.Error.Message}";
                return;
            }

            ApplyScreenData(result.Value);
        }
        catch (Exception ex)
        {
            StatusText = $"Unable to remove the shopping-list row: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            ShoppingListScreenData screenData = await _queryService.GetScreenDataAsync().ConfigureAwait(false);
            ApplyScreenData(screenData);
        }
        catch (Exception ex)
        {
            StatusText = $"Unable to load the shopping list: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyScreenData(ShoppingListScreenData screenData)
    {
        Items = screenData.Items;
        ItemCount = screenData.ItemCount;
        TotalQuantity = screenData.TotalQuantity;
        TotalCost = screenData.TotalCost;
        StatusText = screenData.StatusText;
    }
}