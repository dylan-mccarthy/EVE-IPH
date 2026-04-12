using CommunityToolkit.Mvvm.ComponentModel;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.UI.Avalonia.Services;

namespace EVE.IPH.UI.Avalonia.ViewModels;

public sealed class MarketPriceViewModel : ObservableObject
{
    private readonly IMarketPriceQueryService _queryService;
    private readonly IMarketPriceCommandService _commandService;
    private IReadOnlyList<MarketPriceSourceOption> _sourceOptions = [];
    private MarketPriceSourceOption? _selectedSource;
    private IReadOnlyList<MarketPriceRow> _items = [];
    private string _typeIdsText = string.Empty;
    private long _regionId;
    private string _statusText = "Loading market workspace...";
    private bool _isBusy;

    public MarketPriceViewModel(IMarketPriceQueryService queryService, IMarketPriceCommandService commandService)
    {
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
        LoadTask = LoadAsync();
    }

    public Task LoadTask { get; }

    public IReadOnlyList<MarketPriceSourceOption> SourceOptions
    {
        get => _sourceOptions;
        private set => SetProperty(ref _sourceOptions, value);
    }

    public MarketPriceSourceOption? SelectedSource
    {
        get => _selectedSource;
        set => SetProperty(ref _selectedSource, value);
    }

    public IReadOnlyList<MarketPriceRow> Items
    {
        get => _items;
        private set => SetProperty(ref _items, value);
    }

    public string TypeIdsText
    {
        get => _typeIdsText;
        set => SetProperty(ref _typeIdsText, value);
    }

    public long RegionId
    {
        get => _regionId;
        set => SetProperty(ref _regionId, value);
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
                OnPropertyChanged(nameof(CanBuildWatchlist));
                OnPropertyChanged(nameof(CanLoadPrices));
            }
        }
    }

    public bool CanRefresh => !IsBusy;

    public bool CanBuildWatchlist => !IsBusy;

    public bool CanLoadPrices => !IsBusy && SelectedSource is not null;

    public async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        await LoadAsync().ConfigureAwait(false);
    }

    public async Task LoadPricesAsync()
    {
        if (!CanLoadPrices)
        {
            return;
        }

        try
        {
            IsBusy = true;

            Result<MarketPriceResult> result = await _commandService
                .LoadPricesAsync(new MarketPriceRequest(RegionId, TypeIdsText, SelectedSource!.SourceKind))
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Items = [];
                StatusText = $"Unable to load market prices: {result.Error.Message}";
                return;
            }

            Items = result.Value.Rows;
            StatusText = result.Value.StatusText;
        }
        catch (Exception ex)
        {
            Items = [];
            StatusText = $"Unable to load market prices: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task BuildWatchlistFromSavedSelectionAsync()
    {
        if (!CanBuildWatchlist)
        {
            return;
        }

        try
        {
            IsBusy = true;

            Result<MarketPriceWatchlistResult> result = await _commandService
                .BuildWatchlistFromSavedSelectionAsync()
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                StatusText = $"Unable to build market watchlist: {result.Error.Message}";
                return;
            }

            TypeIdsText = result.Value.TypeIdsText;
            Items = [];
            StatusText = result.Value.StatusText;
        }
        catch (Exception ex)
        {
            StatusText = $"Unable to build market watchlist: {ex.Message}";
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
            MarketPriceScreenData screenData = await _queryService.GetScreenDataAsync().ConfigureAwait(false);
            SourceOptions = screenData.SourceOptions;
            SelectedSource = SourceOptions.FirstOrDefault(option => option.SourceKind == screenData.SelectedSource) ?? SourceOptions.FirstOrDefault();
            RegionId = screenData.RegionId;
            TypeIdsText = screenData.TypeIdsText;
            Items = [];
            StatusText = screenData.StatusText;
        }
        catch (Exception ex)
        {
            SourceOptions = [];
            SelectedSource = null;
            Items = [];
            StatusText = $"Unable to load market workspace: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}