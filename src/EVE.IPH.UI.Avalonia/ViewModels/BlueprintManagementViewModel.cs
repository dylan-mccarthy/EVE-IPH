using CommunityToolkit.Mvvm.ComponentModel;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.UI.Avalonia.Services;

namespace EVE.IPH.UI.Avalonia.ViewModels;

public sealed class BlueprintManagementViewModel : ObservableObject
{
    private readonly IBlueprintManagementQueryService _blueprintManagementQueryService;
    private readonly IBlueprintManagementCommandService _blueprintManagementCommandService;
    private IReadOnlyList<BlueprintManagementRow> _allBlueprints = [];
    private IReadOnlyList<BlueprintManagementRow> _items = [];
    private IReadOnlyList<BlueprintOwnerFilterOption> _ownerOptions = [new BlueprintOwnerFilterOption(null, "All Owners")];
    private BlueprintOwnerFilterOption? _selectedOwner = new(null, "All Owners");
    private BlueprintManagementRow? _selectedBlueprint;
    private string _searchText = string.Empty;
    private string _baseStatusText = "Loading blueprints...";
    private string _statusText = "Loading blueprints...";
    private bool _isRefreshing;
    private string _editBlueprintName = string.Empty;
    private int _editQuantity = 1;
    private int _editMe;
    private int _editTe;
    private int _editRuns = -1;

    public BlueprintManagementViewModel(
        IBlueprintManagementQueryService blueprintManagementQueryService,
        IBlueprintManagementCommandService blueprintManagementCommandService)
    {
        _blueprintManagementQueryService = blueprintManagementQueryService ?? throw new ArgumentNullException(nameof(blueprintManagementQueryService));
        _blueprintManagementCommandService = blueprintManagementCommandService ?? throw new ArgumentNullException(nameof(blueprintManagementCommandService));

        LoadTask = LoadAsync();
    }

    public Task LoadTask { get; }

    public IReadOnlyList<BlueprintOwnerFilterOption> OwnerOptions
    {
        get => _ownerOptions;
        private set => SetProperty(ref _ownerOptions, value);
    }

    public BlueprintOwnerFilterOption? SelectedOwner
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

    public IReadOnlyList<BlueprintManagementRow> Items
    {
        get => _items;
        private set => SetProperty(ref _items, value);
    }

    public BlueprintManagementRow? SelectedBlueprint
    {
        get => _selectedBlueprint;
        set
        {
            if (SetProperty(ref _selectedBlueprint, value))
            {
                ApplySelectedBlueprint(value);
                OnPropertyChanged(nameof(CanEditSelectedBlueprint));
            }
        }
    }

    public string EditBlueprintName
    {
        get => _editBlueprintName;
        set => SetProperty(ref _editBlueprintName, value);
    }

    public int EditQuantity
    {
        get => _editQuantity;
        set => SetProperty(ref _editQuantity, value);
    }

    public int EditMe
    {
        get => _editMe;
        set => SetProperty(ref _editMe, value);
    }

    public int EditTe
    {
        get => _editTe;
        set => SetProperty(ref _editTe, value);
    }

    public int EditRuns
    {
        get => _editRuns;
        set => SetProperty(ref _editRuns, value);
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
                OnPropertyChanged(nameof(CanEditSelectedBlueprint));
            }
        }
    }

    public bool CanRefresh => !IsRefreshing;

    public bool CanEditSelectedBlueprint => !IsRefreshing && SelectedBlueprint is not null;

    public async Task RefreshAsync()
    {
        if (IsRefreshing)
        {
            return;
        }

        try
        {
            IsRefreshing = true;

            BlueprintManagementScreenData screenData = await _blueprintManagementCommandService
                .RefreshAsync()
                .ConfigureAwait(false);

            ApplyScreenData(screenData, SelectedBlueprint?.OwnerId, SelectedBlueprint?.BlueprintId);
        }
        catch (Exception ex)
        {
            StatusText = $"Unable to refresh blueprints: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    public async Task SaveSelectedBlueprintAsync()
    {
        if (SelectedBlueprint is null || IsRefreshing)
        {
            return;
        }

        try
        {
            IsRefreshing = true;

            OwnedBlueprintRecord updatedBlueprint = new(
                SelectedBlueprint.OwnerId,
                SelectedBlueprint.ItemId,
                SelectedBlueprint.LocationId,
                SelectedBlueprint.BlueprintId,
                string.IsNullOrWhiteSpace(EditBlueprintName) ? SelectedBlueprint.BlueprintName : EditBlueprintName.Trim(),
                Math.Max(1, EditQuantity),
                EditMe,
                EditTe,
                EditRuns,
                SelectedBlueprint.BpType,
                SelectedBlueprint.Owned,
                SelectedBlueprint.Scanned);

            var saveResult = await _blueprintManagementCommandService
                .SaveBlueprintAsync(updatedBlueprint)
                .ConfigureAwait(false);

            if (saveResult.IsFailure)
            {
                StatusText = $"Unable to save blueprint: {saveResult.Error.Message}";
                return;
            }

            await ReloadAsync(updatedBlueprint.UserId, updatedBlueprint.BlueprintId).ConfigureAwait(false);
            StatusText = $"Saved blueprint {saveResult.Value.BlueprintName}.";
        }
        catch (Exception ex)
        {
            StatusText = $"Unable to save blueprint: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    public async Task DeleteSelectedBlueprintAsync()
    {
        if (SelectedBlueprint is null || IsRefreshing)
        {
            return;
        }

        long ownerId = SelectedBlueprint.OwnerId;
        BlueprintId blueprintId = SelectedBlueprint.BlueprintId;
        string blueprintName = SelectedBlueprint.BlueprintName;

        try
        {
            IsRefreshing = true;

            var deleteResult = await _blueprintManagementCommandService
                .DeleteBlueprintAsync(ownerId, blueprintId)
                .ConfigureAwait(false);

            if (deleteResult.IsFailure)
            {
                StatusText = $"Unable to delete blueprint: {deleteResult.Error.Message}";
                return;
            }

            await ReloadAsync(ownerId, null).ConfigureAwait(false);
            StatusText = deleteResult.Value
                ? $"Deleted blueprint {blueprintName}."
                : $"Blueprint {blueprintName} was not found to delete.";
        }
        catch (Exception ex)
        {
            StatusText = $"Unable to delete blueprint: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task LoadAsync()
    {
        if (IsRefreshing)
        {
            return;
        }

        try
        {
            IsRefreshing = true;
            await ReloadAsync(null, null).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusText = $"Unable to load blueprints: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task ReloadAsync(long? preferredOwnerId, BlueprintId? preferredBlueprintId)
    {
        BlueprintManagementScreenData screenData = await _blueprintManagementQueryService
            .GetScreenDataAsync()
            .ConfigureAwait(false);

        ApplyScreenData(screenData, preferredOwnerId, preferredBlueprintId);
    }

    private void ApplyScreenData(BlueprintManagementScreenData screenData, long? preferredOwnerId, BlueprintId? preferredBlueprintId)
    {
        _allBlueprints = screenData.Blueprints;
        long? previousOwnerId = preferredOwnerId ?? SelectedOwner?.OwnerId;

        OwnerOptions = screenData.OwnerOptions.Count == 0
            ? [new BlueprintOwnerFilterOption(null, "All Owners")]
            : screenData.OwnerOptions;

        SelectedOwner = OwnerOptions.FirstOrDefault(option => option.OwnerId == previousOwnerId) ?? OwnerOptions.FirstOrDefault();
        _baseStatusText = screenData.StatusText;
        ApplyFilters();

        if (preferredBlueprintId is not null)
        {
            SelectedBlueprint = Items.FirstOrDefault(item => item.OwnerId == (preferredOwnerId ?? item.OwnerId) && item.BlueprintId == preferredBlueprintId);
        }
        else if (SelectedBlueprint is not null)
        {
            SelectedBlueprint = Items.FirstOrDefault(item => item.OwnerId == SelectedBlueprint.OwnerId && item.BlueprintId == SelectedBlueprint.BlueprintId);
        }

        SelectedBlueprint ??= Items.FirstOrDefault();
    }

    private void ApplySelectedBlueprint(BlueprintManagementRow? blueprint)
    {
        if (blueprint is null)
        {
            EditBlueprintName = string.Empty;
            EditQuantity = 1;
            EditMe = 0;
            EditTe = 0;
            EditRuns = -1;
            return;
        }

        EditBlueprintName = blueprint.BlueprintName;
        EditQuantity = blueprint.Quantity;
        EditMe = blueprint.Me;
        EditTe = blueprint.Te;
        EditRuns = blueprint.Runs;
    }

    private void ApplyFilters()
    {
        IEnumerable<BlueprintManagementRow> filtered = _allBlueprints;

        if (SelectedOwner?.OwnerId is long ownerId)
        {
            filtered = filtered.Where(item => item.OwnerId == ownerId);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string searchText = SearchText.Trim();
            filtered = filtered.Where(item =>
                item.BlueprintName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                || item.OwnerName.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        Items = filtered
            .OrderBy(item => item.OwnerName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.BlueprintName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (_allBlueprints.Count == 0)
        {
            StatusText = _baseStatusText;
            return;
        }

        string ownerScope = SelectedOwner?.OwnerId is long ? $" for {SelectedOwner.DisplayName}" : string.Empty;
        StatusText = $"{_baseStatusText} Showing {Items.Count} of {_allBlueprints.Count} owned blueprints{ownerScope}.";
    }
}