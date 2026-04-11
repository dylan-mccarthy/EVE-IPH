using CommunityToolkit.Mvvm.ComponentModel;
using EVE.IPH.Domain.Assets.Models;
using EVE.IPH.Domain.Assets.Services;
using EVE.IPH.UI.Avalonia.Services;

namespace EVE.IPH.UI.Avalonia.ViewModels;

public sealed class AssetsViewModel : ObservableObject
{
    private readonly IAssetViewFilterService _assetViewFilterService;
    private readonly IAssetsQueryService _assetsQueryService;
    private readonly IAssetsCommandService _assetsCommandService;
    private IReadOnlyList<HydratedAsset> _allAssets = [];
    private IReadOnlyList<HydratedAsset> _items = [];
    private IReadOnlyList<AssetOwnerFilterOption> _ownerOptions = [new AssetOwnerFilterOption(null, "All Owners")];
    private AssetOwnerFilterOption? _selectedOwner = new(null, "All Owners");
    private string _searchText = string.Empty;
    private bool _onlyBlueprintCopies;
    private AssetSortMode _selectedSortMode = AssetSortMode.Name;
    private string _baseStatusText = "Loading assets...";
    private string _statusText = "Loading assets...";
    private bool _isRefreshing;

    public AssetsViewModel(
        IAssetViewFilterService assetViewFilterService,
        IAssetsQueryService assetsQueryService,
        IAssetsCommandService assetsCommandService)
    {
        _assetViewFilterService = assetViewFilterService ?? throw new ArgumentNullException(nameof(assetViewFilterService));
        _assetsQueryService = assetsQueryService ?? throw new ArgumentNullException(nameof(assetsQueryService));
        _assetsCommandService = assetsCommandService ?? throw new ArgumentNullException(nameof(assetsCommandService));

        SortModes = Enum.GetValues<AssetSortMode>();
        LoadTask = LoadAsync();
    }

    public Task LoadTask { get; }

    public IReadOnlyList<AssetSortMode> SortModes { get; }

    public IReadOnlyList<AssetOwnerFilterOption> OwnerOptions
    {
        get => _ownerOptions;
        private set => SetProperty(ref _ownerOptions, value);
    }

    public AssetOwnerFilterOption? SelectedOwner
    {
        get => _selectedOwner;
        set
        {
            if (SetProperty(ref _selectedOwner, value))
            {
                ApplyFilters();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilters();
            }
        }
    }

    public bool OnlyBlueprintCopies
    {
        get => _onlyBlueprintCopies;
        set
        {
            if (SetProperty(ref _onlyBlueprintCopies, value))
            {
                ApplyFilters();
            }
        }
    }

    public AssetSortMode SelectedSortMode
    {
        get => _selectedSortMode;
        set
        {
            if (SetProperty(ref _selectedSortMode, value))
            {
                ApplyFilters();
            }
        }
    }

    public IReadOnlyList<HydratedAsset> Items
    {
        get => _items;
        private set => SetProperty(ref _items, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set
        {
            if (SetProperty(ref _isRefreshing, value))
            {
                OnPropertyChanged(nameof(CanRefresh));
            }
        }
    }

    public bool CanRefresh => !IsRefreshing;

    public async Task RefreshAsync()
    {
        if (IsRefreshing)
        {
            return;
        }

        try
        {
            IsRefreshing = true;
            AssetsScreenData screenData = await _assetsCommandService.RefreshAsync().ConfigureAwait(false);
            ApplyScreenData(screenData);
        }
        catch (Exception ex)
        {
            StatusText = $"Unable to refresh assets: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            IsRefreshing = true;
            AssetsScreenData screenData = await _assetsQueryService.GetScreenDataAsync().ConfigureAwait(false);
            ApplyScreenData(screenData);
        }
        catch (Exception ex)
        {
            StatusText = $"Unable to load assets: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private void ApplyScreenData(AssetsScreenData screenData)
    {
        _allAssets = screenData.Assets;
        AssetOwnerFilterOption? previouslySelectedOwner = SelectedOwner;

        OwnerOptions = screenData.OwnerOptions.Count == 0
            ? [new AssetOwnerFilterOption(null, "All Owners")]
            : screenData.OwnerOptions;

        SelectedOwner = previouslySelectedOwner is not null
            ? OwnerOptions.FirstOrDefault(option => option.OwnerId == previouslySelectedOwner.OwnerId)
            : OwnerOptions.FirstOrDefault();

        if (SelectedOwner is null)
        {
            SelectedOwner = new AssetOwnerFilterOption(null, "All Owners");
        }

        _baseStatusText = screenData.StatusText;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        AssetViewRequest request = new(
            OwnerIds: SelectedOwner?.OwnerId is long ownerId ? new HashSet<long> { ownerId } : new HashSet<long>(),
            TypeIds: new HashSet<long>(),
            SearchText: SearchText,
            OnlyBlueprintCopies: OnlyBlueprintCopies,
            SortMode: SelectedSortMode);

        Items = _assetViewFilterService.Filter(_allAssets, request);

        if (_allAssets.Count == 0)
        {
            StatusText = _baseStatusText;
            return;
        }

        string ownerScope = SelectedOwner?.OwnerId is long ? $" for {SelectedOwner.DisplayName}" : string.Empty;
        StatusText = $"{_baseStatusText} Showing {Items.Count} of {_allAssets.Count} hydrated assets{ownerScope}.";
    }
}