using CommunityToolkit.Mvvm.ComponentModel;
using EVE.IPH.Domain.Assets.Models;
using EVE.IPH.Domain.Assets.Services;
using EVE.IPH.UI.Avalonia.Services;

namespace EVE.IPH.UI.Avalonia.ViewModels;

public sealed class AssetsViewModel : ObservableObject
{
    private readonly IAssetViewFilterService _assetViewFilterService;
    private readonly IReadOnlyList<HydratedAsset> _allAssets;
    private IReadOnlyList<HydratedAsset> _items = [];
    private string _searchText = string.Empty;
    private bool _onlyBlueprintCopies;
    private AssetSortMode _selectedSortMode = AssetSortMode.Name;
    private string _statusText = string.Empty;

    public AssetsViewModel(
        IAssetViewFilterService assetViewFilterService,
        IAssetsScreenService assetsScreenService)
    {
        _assetViewFilterService = assetViewFilterService ?? throw new ArgumentNullException(nameof(assetViewFilterService));
        ArgumentNullException.ThrowIfNull(assetsScreenService);

        _allAssets = assetsScreenService.GetHydratedAssets();

        SortModes = Enum.GetValues<AssetSortMode>();
        Refresh();
    }

    public IReadOnlyList<AssetSortMode> SortModes { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                Refresh();
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
                Refresh();
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
                Refresh();
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

    private void Refresh()
    {
        AssetViewRequest request = new(
            OwnerIds: new HashSet<long>(),
            TypeIds: new HashSet<long>(),
            SearchText: SearchText,
            OnlyBlueprintCopies: OnlyBlueprintCopies,
            SortMode: SelectedSortMode);

        Items = _assetViewFilterService.Filter(_allAssets, request);
        StatusText = $"Showing {Items.Count} of {_allAssets.Count} hydrated assets using the extracted Phase 10 filters.";
    }
}