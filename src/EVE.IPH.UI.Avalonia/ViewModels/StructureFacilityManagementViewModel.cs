using CommunityToolkit.Mvvm.ComponentModel;
using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.UI.Avalonia.Services;

namespace EVE.IPH.UI.Avalonia.ViewModels;

public sealed class StructureFacilityManagementViewModel : ObservableObject
{
    private readonly IStructureFacilityManagementQueryService _queryService;
    private readonly IStructureFacilityManagementCommandService _commandService;
    private IReadOnlyList<StructureFacilityCharacterOption> _characters = [];
    private IReadOnlyList<IndustryStructureRow> _structures = [];
    private IReadOnlyList<FacilitySettingsRow> _allFacilities = [];
    private IReadOnlyList<FacilitySettingsRow> _facilityItems = [];
    private IReadOnlyList<FacilityProductionTypeOption> _productionTypes = [];
    private IReadOnlyList<FacilityKindOption> _facilityKinds = [];
    private StructureFacilityCharacterOption? _selectedCharacter;
    private IndustryStructureRow? _selectedStructure;
    private FacilitySettingsRow? _selectedFacility;
    private FacilityProductionTypeOption? _selectedProductionType;
    private FacilityKindOption? _selectedFacilityKind;
    private string _statusText = "Loading saved structures and facility settings...";
    private bool _isBusy;

    private long _structureId;
    private string _structureName = string.Empty;
    private int _structureTypeId;
    private long _structureSolarSystemId;
    private long _structureRegionId;
    private string _structureOwnerCorporationIdText = string.Empty;
    private bool _structureIsManualEntry = true;

    private long _facilityId;
    private string _facilityName = string.Empty;
    private long _facilityRegionId;
    private string _facilityRegionName = string.Empty;
    private long _facilitySolarSystemId;
    private string _facilitySolarSystemName = string.Empty;
    private double _facilitySolarSystemSecurity;
    private double _facilityCostIndex;
    private double _facilityActivityCostPerSecond;
    private bool _includeActivityCost = true;
    private bool _includeActivityTime = true;
    private bool _includeActivityUsage = true;
    private bool _convertToOre;
    private int _factionWarfareUpgradeLevel = -1;
    private double _facilityTaxRate;
    private string _materialMultiplierOverrideText = string.Empty;
    private string _timeMultiplierOverrideText = string.Empty;
    private string _costMultiplierOverrideText = string.Empty;
    private string _moduleTypeIdsText = string.Empty;

    public StructureFacilityManagementViewModel(
        IStructureFacilityManagementQueryService queryService,
        IStructureFacilityManagementCommandService commandService)
    {
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
        LoadTask = LoadAsync();
    }

    public Task LoadTask { get; }

    public IReadOnlyList<StructureFacilityCharacterOption> Characters
    {
        get => _characters;
        private set => SetProperty(ref _characters, value);
    }

    public IReadOnlyList<IndustryStructureRow> Structures
    {
        get => _structures;
        private set => SetProperty(ref _structures, value);
    }

    public IReadOnlyList<FacilitySettingsRow> FacilityItems
    {
        get => _facilityItems;
        private set => SetProperty(ref _facilityItems, value);
    }

    public IReadOnlyList<FacilityProductionTypeOption> ProductionTypes
    {
        get => _productionTypes;
        private set => SetProperty(ref _productionTypes, value);
    }

    public IReadOnlyList<FacilityKindOption> FacilityKinds
    {
        get => _facilityKinds;
        private set => SetProperty(ref _facilityKinds, value);
    }

    public StructureFacilityCharacterOption? SelectedCharacter
    {
        get => _selectedCharacter;
        set
        {
            if (SetProperty(ref _selectedCharacter, value))
            {
                ApplyFacilityFilter();
                OnPropertyChanged(nameof(CanSaveFacility));
                OnPropertyChanged(nameof(CanDeleteFacility));
            }
        }
    }

    public IndustryStructureRow? SelectedStructure
    {
        get => _selectedStructure;
        set
        {
            if (SetProperty(ref _selectedStructure, value))
            {
                ApplySelectedStructure(value);
                OnPropertyChanged(nameof(CanDeleteStructure));
                OnPropertyChanged(nameof(CanUseSelectedStructure));
            }
        }
    }

    public FacilitySettingsRow? SelectedFacility
    {
        get => _selectedFacility;
        set
        {
            if (SetProperty(ref _selectedFacility, value))
            {
                ApplySelectedFacility(value);
                OnPropertyChanged(nameof(CanDeleteFacility));
            }
        }
    }

    public FacilityProductionTypeOption? SelectedProductionType
    {
        get => _selectedProductionType;
        set
        {
            if (SetProperty(ref _selectedProductionType, value))
            {
                ApplyFacilitySelectionForCurrentContext();
                OnPropertyChanged(nameof(CanSaveFacility));
            }
        }
    }

    public FacilityKindOption? SelectedFacilityKind
    {
        get => _selectedFacilityKind;
        set => SetProperty(ref _selectedFacilityKind, value);
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
                OnPropertyChanged(nameof(CanSaveStructure));
                OnPropertyChanged(nameof(CanDeleteStructure));
                OnPropertyChanged(nameof(CanUseSelectedStructure));
                OnPropertyChanged(nameof(CanSaveFacility));
                OnPropertyChanged(nameof(CanDeleteFacility));
            }
        }
    }

    public bool CanRefresh => !IsBusy;

    public bool CanSaveStructure => !IsBusy;

    public bool CanDeleteStructure => !IsBusy && SelectedStructure is not null;

    public bool CanUseSelectedStructure => !IsBusy && SelectedStructure is not null;

    public bool CanSaveFacility => !IsBusy && SelectedCharacter is not null && SelectedProductionType is not null;

    public bool CanDeleteFacility => !IsBusy && SelectedCharacter is not null && SelectedProductionType is not null && SelectedFacility is not null;

    public long StructureId
    {
        get => _structureId;
        set => SetProperty(ref _structureId, value);
    }

    public string StructureName
    {
        get => _structureName;
        set => SetProperty(ref _structureName, value);
    }

    public int StructureTypeId
    {
        get => _structureTypeId;
        set => SetProperty(ref _structureTypeId, value);
    }

    public long StructureSolarSystemId
    {
        get => _structureSolarSystemId;
        set => SetProperty(ref _structureSolarSystemId, value);
    }

    public long StructureRegionId
    {
        get => _structureRegionId;
        set => SetProperty(ref _structureRegionId, value);
    }

    public string StructureOwnerCorporationIdText
    {
        get => _structureOwnerCorporationIdText;
        set => SetProperty(ref _structureOwnerCorporationIdText, value);
    }

    public bool StructureIsManualEntry
    {
        get => _structureIsManualEntry;
        set => SetProperty(ref _structureIsManualEntry, value);
    }

    public long FacilityId
    {
        get => _facilityId;
        set => SetProperty(ref _facilityId, value);
    }

    public string FacilityName
    {
        get => _facilityName;
        set => SetProperty(ref _facilityName, value);
    }

    public long FacilityRegionId
    {
        get => _facilityRegionId;
        set => SetProperty(ref _facilityRegionId, value);
    }

    public string FacilityRegionName
    {
        get => _facilityRegionName;
        set => SetProperty(ref _facilityRegionName, value);
    }

    public long FacilitySolarSystemId
    {
        get => _facilitySolarSystemId;
        set => SetProperty(ref _facilitySolarSystemId, value);
    }

    public string FacilitySolarSystemName
    {
        get => _facilitySolarSystemName;
        set => SetProperty(ref _facilitySolarSystemName, value);
    }

    public double FacilitySolarSystemSecurity
    {
        get => _facilitySolarSystemSecurity;
        set => SetProperty(ref _facilitySolarSystemSecurity, value);
    }

    public double FacilityCostIndex
    {
        get => _facilityCostIndex;
        set => SetProperty(ref _facilityCostIndex, value);
    }

    public double FacilityActivityCostPerSecond
    {
        get => _facilityActivityCostPerSecond;
        set => SetProperty(ref _facilityActivityCostPerSecond, value);
    }

    public bool IncludeActivityCost
    {
        get => _includeActivityCost;
        set => SetProperty(ref _includeActivityCost, value);
    }

    public bool IncludeActivityTime
    {
        get => _includeActivityTime;
        set => SetProperty(ref _includeActivityTime, value);
    }

    public bool IncludeActivityUsage
    {
        get => _includeActivityUsage;
        set => SetProperty(ref _includeActivityUsage, value);
    }

    public bool ConvertToOre
    {
        get => _convertToOre;
        set => SetProperty(ref _convertToOre, value);
    }

    public int FactionWarfareUpgradeLevel
    {
        get => _factionWarfareUpgradeLevel;
        set => SetProperty(ref _factionWarfareUpgradeLevel, value);
    }

    public double FacilityTaxRate
    {
        get => _facilityTaxRate;
        set => SetProperty(ref _facilityTaxRate, value);
    }

    public string MaterialMultiplierOverrideText
    {
        get => _materialMultiplierOverrideText;
        set => SetProperty(ref _materialMultiplierOverrideText, value);
    }

    public string TimeMultiplierOverrideText
    {
        get => _timeMultiplierOverrideText;
        set => SetProperty(ref _timeMultiplierOverrideText, value);
    }

    public string CostMultiplierOverrideText
    {
        get => _costMultiplierOverrideText;
        set => SetProperty(ref _costMultiplierOverrideText, value);
    }

    public string ModuleTypeIdsText
    {
        get => _moduleTypeIdsText;
        set => SetProperty(ref _moduleTypeIdsText, value);
    }

    public async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        await LoadAsync().ConfigureAwait(false);
    }

    public void UseSelectedStructureForFacility()
    {
        if (SelectedStructure is null)
        {
            return;
        }

        SelectedFacilityKind = FacilityKinds.FirstOrDefault(option => option.FacilityKind == IndustryFacilityKind.UpwellStructure);
        FacilityId = SelectedStructure.StructureId;
        FacilityName = SelectedStructure.StructureName;
        FacilityRegionId = SelectedStructure.RegionId;
        FacilitySolarSystemId = SelectedStructure.SolarSystemId;
        StatusText = $"Loaded structure {SelectedStructure.StructureName} into the facility form.";
    }

    public async Task SaveStructureAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var result = await _commandService.SaveStructureAsync(new StructureUpsertRequest(
                StructureId,
                StructureName,
                StructureTypeId,
                StructureSolarSystemId,
                StructureRegionId,
                ParseNullableLong(StructureOwnerCorporationIdText),
                StructureIsManualEntry)).ConfigureAwait(false);

            if (result.IsFailure)
            {
                StatusText = $"Unable to save structure: {result.Error.Message}";
                return;
            }

            await ReloadAsync(SelectedCharacter?.CharacterId.Value, result.Value.StructureId, SelectedProductionType?.ProductionType).ConfigureAwait(false);
            StatusText = $"Saved structure {result.Value.StructureName}.";
        }
        catch (Exception ex)
        {
            StatusText = $"Unable to save structure: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task DeleteSelectedStructureAsync()
    {
        if (SelectedStructure is null || IsBusy)
        {
            return;
        }

        long structureId = SelectedStructure.StructureId;
        string structureName = SelectedStructure.StructureName;

        try
        {
            IsBusy = true;
            var result = await _commandService.DeleteStructureAsync(structureId).ConfigureAwait(false);
            if (result.IsFailure)
            {
                StatusText = $"Unable to delete structure: {result.Error.Message}";
                return;
            }

            await ReloadAsync(SelectedCharacter?.CharacterId.Value, null, SelectedProductionType?.ProductionType).ConfigureAwait(false);
            StatusText = result.Value
                ? $"Deleted structure {structureName}."
                : $"Structure {structureName} was not found to delete.";
        }
        catch (Exception ex)
        {
            StatusText = $"Unable to delete structure: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SaveFacilityAsync()
    {
        if (!CanSaveFacility)
        {
            return;
        }

        try
        {
            IsBusy = true;

            var result = await _commandService.SaveFacilityAsync(new FacilitySettingsUpsertRequest(
                SelectedCharacter!.CharacterId,
                SelectedProductionType!.ProductionType,
                FacilityId,
                FacilityName,
                SelectedFacilityKind?.FacilityKind ?? IndustryFacilityKind.Station,
                FacilityRegionId,
                FacilityRegionName,
                FacilitySolarSystemId,
                FacilitySolarSystemName,
                FacilitySolarSystemSecurity,
                FacilityCostIndex,
                FacilityActivityCostPerSecond,
                IncludeActivityCost,
                IncludeActivityTime,
                IncludeActivityUsage,
                ConvertToOre,
                FactionWarfareUpgradeLevel,
                FacilityTaxRate,
                ParseNullableDouble(MaterialMultiplierOverrideText),
                ParseNullableDouble(TimeMultiplierOverrideText),
                ParseNullableDouble(CostMultiplierOverrideText),
                ParseModuleTypeIds(ModuleTypeIdsText))).ConfigureAwait(false);

            if (result.IsFailure)
            {
                StatusText = $"Unable to save facility settings: {result.Error.Message}";
                return;
            }

            await ReloadAsync(SelectedCharacter.CharacterId.Value, SelectedStructure?.StructureId, SelectedProductionType.ProductionType).ConfigureAwait(false);
            StatusText = $"Saved {SelectedProductionType.DisplayName} settings for {SelectedCharacter.Name}.";
        }
        catch (Exception ex)
        {
            StatusText = $"Unable to save facility settings: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task DeleteFacilityAsync()
    {
        if (!CanDeleteFacility)
        {
            return;
        }

        try
        {
            IsBusy = true;

            var result = await _commandService.DeleteFacilityAsync(SelectedCharacter!.CharacterId, SelectedProductionType!.ProductionType).ConfigureAwait(false);
            if (result.IsFailure)
            {
                StatusText = $"Unable to delete facility settings: {result.Error.Message}";
                return;
            }

            string characterName = SelectedCharacter.Name;
            string productionTypeName = SelectedProductionType.DisplayName;
            await ReloadAsync(SelectedCharacter.CharacterId.Value, SelectedStructure?.StructureId, SelectedProductionType.ProductionType).ConfigureAwait(false);
            StatusText = result.Value
                ? $"Deleted {productionTypeName} settings for {characterName}."
                : $"No saved {productionTypeName} settings were found for {characterName}.";
        }
        catch (Exception ex)
        {
            StatusText = $"Unable to delete facility settings: {ex.Message}";
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
            await ReloadAsync(SelectedCharacter?.CharacterId.Value, SelectedStructure?.StructureId, SelectedProductionType?.ProductionType).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusText = $"Unable to load structure and facility settings: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReloadAsync(long? preferredCharacterId, long? preferredStructureId, FacilityProductionType? preferredProductionType)
    {
        StructureFacilityManagementScreenData screenData = await _queryService.GetScreenDataAsync().ConfigureAwait(false);
        ApplyScreenData(screenData, preferredCharacterId, preferredStructureId, preferredProductionType);
    }

    private void ApplyScreenData(StructureFacilityManagementScreenData screenData, long? preferredCharacterId, long? preferredStructureId, FacilityProductionType? preferredProductionType)
    {
        Characters = screenData.Characters;
        Structures = screenData.Structures;
        _allFacilities = screenData.Facilities;
        ProductionTypes = screenData.ProductionTypes;
        FacilityKinds = screenData.FacilityKinds;

        SelectedCharacter = Characters.FirstOrDefault(character => character.CharacterId.Value == preferredCharacterId)
            ?? Characters.FirstOrDefault(character => character.IsDefault)
            ?? Characters.FirstOrDefault();

        SelectedStructure = Structures.FirstOrDefault(structure => structure.StructureId == preferredStructureId)
            ?? Structures.FirstOrDefault();

        SelectedProductionType = ProductionTypes.FirstOrDefault(option => option.ProductionType == preferredProductionType)
            ?? ProductionTypes.FirstOrDefault();

        StatusText = screenData.StatusText;
    }

    private void ApplyFacilityFilter()
    {
        if (SelectedCharacter is null)
        {
            FacilityItems = [];
            SelectedFacility = null;
            ClearFacilityForm();
            return;
        }

        FacilityItems = _allFacilities
            .Where(facility => facility.CharacterId == SelectedCharacter.CharacterId)
            .OrderBy(facility => facility.ProductionTypeDisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        ApplyFacilitySelectionForCurrentContext();
    }

    private void ApplyFacilitySelectionForCurrentContext()
    {
        if (SelectedCharacter is null || SelectedProductionType is null)
        {
            SelectedFacility = null;
            ClearFacilityForm();
            return;
        }

        SelectedFacility = FacilityItems.FirstOrDefault(facility => facility.ProductionType == SelectedProductionType.ProductionType);
        if (SelectedFacility is null)
        {
            ClearFacilityForm();
            SelectedFacilityKind = FacilityKinds.FirstOrDefault(option => option.FacilityKind == IndustryFacilityKind.Station) ?? FacilityKinds.FirstOrDefault();
        }
    }

    private void ApplySelectedStructure(IndustryStructureRow? structure)
    {
        if (structure is null)
        {
            StructureId = 0;
            StructureName = string.Empty;
            StructureTypeId = 0;
            StructureSolarSystemId = 0;
            StructureRegionId = 0;
            StructureOwnerCorporationIdText = string.Empty;
            StructureIsManualEntry = true;
            return;
        }

        StructureId = structure.StructureId;
        StructureName = structure.StructureName;
        StructureTypeId = structure.StructureTypeId;
        StructureSolarSystemId = structure.SolarSystemId;
        StructureRegionId = structure.RegionId;
        StructureOwnerCorporationIdText = structure.OwnerCorporationId?.ToString() ?? string.Empty;
        StructureIsManualEntry = structure.IsManualEntry;
    }

    private void ApplySelectedFacility(FacilitySettingsRow? facility)
    {
        if (facility is null)
        {
            ClearFacilityForm();
            return;
        }

        SelectedProductionType = ProductionTypes.FirstOrDefault(option => option.ProductionType == facility.ProductionType) ?? SelectedProductionType;
        SelectedFacilityKind = FacilityKinds.FirstOrDefault(option => option.FacilityKind == facility.FacilityKind) ?? SelectedFacilityKind;
        FacilityId = facility.FacilityId;
        FacilityName = facility.FacilityName;
        FacilityRegionId = facility.RegionId;
        FacilityRegionName = facility.RegionName;
        FacilitySolarSystemId = facility.SolarSystemId;
        FacilitySolarSystemName = facility.SolarSystemName;
        FacilitySolarSystemSecurity = facility.SolarSystemSecurity;
        FacilityCostIndex = facility.CostIndex;
        FacilityActivityCostPerSecond = facility.ActivityCostPerSecond;
        IncludeActivityCost = facility.IncludeActivityCost;
        IncludeActivityTime = facility.IncludeActivityTime;
        IncludeActivityUsage = facility.IncludeActivityUsage;
        ConvertToOre = facility.ConvertToOre;
        FactionWarfareUpgradeLevel = facility.FactionWarfareUpgradeLevel;
        FacilityTaxRate = facility.TaxRate;
        MaterialMultiplierOverrideText = facility.MaterialMultiplierOverride?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
        TimeMultiplierOverrideText = facility.TimeMultiplierOverride?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
        CostMultiplierOverrideText = facility.CostMultiplierOverride?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
        ModuleTypeIdsText = facility.InstalledModuleTypeIds.Count == 0 ? string.Empty : string.Join(", ", facility.InstalledModuleTypeIds);
    }

    private void ClearFacilityForm()
    {
        FacilityId = 0;
        FacilityName = string.Empty;
        FacilityRegionId = 0;
        FacilityRegionName = string.Empty;
        FacilitySolarSystemId = 0;
        FacilitySolarSystemName = string.Empty;
        FacilitySolarSystemSecurity = 0;
        FacilityCostIndex = 0;
        FacilityActivityCostPerSecond = 0;
        IncludeActivityCost = true;
        IncludeActivityTime = true;
        IncludeActivityUsage = true;
        ConvertToOre = false;
        FactionWarfareUpgradeLevel = -1;
        FacilityTaxRate = 0;
        MaterialMultiplierOverrideText = string.Empty;
        TimeMultiplierOverrideText = string.Empty;
        CostMultiplierOverrideText = string.Empty;
        ModuleTypeIdsText = string.Empty;
    }

    private static long? ParseNullableLong(string value) => long.TryParse(value?.Trim(), out long parsed) ? parsed : null;

    private static double? ParseNullableDouble(string value) => double.TryParse(value?.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double parsed) ? parsed : null;

    private static IReadOnlyList<int> ParseModuleTypeIds(string moduleTypeIds)
    {
        if (string.IsNullOrWhiteSpace(moduleTypeIds))
        {
            return [];
        }

        return moduleTypeIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out int parsed) ? parsed : 0)
            .Where(value => value > 0)
            .Distinct()
            .OrderBy(value => value)
            .ToArray();
    }
}