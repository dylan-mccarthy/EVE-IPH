using CommunityToolkit.Mvvm.ComponentModel;
using EVE.IPH.UI.Avalonia.Services;

namespace EVE.IPH.UI.Avalonia.ViewModels;

public sealed class ManufacturingWorkspaceViewModel : ObservableObject
{
    private readonly IManufacturingWorkspaceQueryService _manufacturingWorkspaceQueryService;
    private readonly IManufacturingWorkspaceCommandService _manufacturingWorkspaceCommandService;
    private IReadOnlyList<ManufacturingBlueprintOption> _blueprints = [];
    private IReadOnlyList<ManufacturingFacilityOption> _facilities = [];
    private ManufacturingBlueprintOption? _selectedBlueprint;
    private ManufacturingFacilityOption? _selectedFacility;
    private string _statusText = "Loading manufacturing workspace...";
    private bool _isBusy;
    private int _userRuns = 1;
    private double _itemMarketCost;
    private double _rawMaterialsCost;
    private double _componentMaterialsCost;
    private double _additionalCosts;
    private double _estimatedItemValue;
    private bool _applySalesTax = true;
    private bool _includeBrokerFee = true;
    private ManufacturingWorkspaceAnalysisResult? _analysisResult;

    public ManufacturingWorkspaceViewModel(
        IManufacturingWorkspaceQueryService manufacturingWorkspaceQueryService,
        IManufacturingWorkspaceCommandService manufacturingWorkspaceCommandService)
    {
        _manufacturingWorkspaceQueryService = manufacturingWorkspaceQueryService ?? throw new ArgumentNullException(nameof(manufacturingWorkspaceQueryService));
        _manufacturingWorkspaceCommandService = manufacturingWorkspaceCommandService ?? throw new ArgumentNullException(nameof(manufacturingWorkspaceCommandService));

        LoadTask = LoadAsync();
    }

    public Task LoadTask { get; }

    public IReadOnlyList<ManufacturingBlueprintOption> Blueprints
    {
        get => _blueprints;
        private set => SetProperty(ref _blueprints, value);
    }

    public IReadOnlyList<ManufacturingFacilityOption> Facilities
    {
        get => _facilities;
        private set => SetProperty(ref _facilities, value);
    }

    public ManufacturingBlueprintOption? SelectedBlueprint
    {
        get => _selectedBlueprint;
        set
        {
            if (SetProperty(ref _selectedBlueprint, value))
            {
                if (value is not null)
                {
                    UserRuns = value.Runs > 0 ? value.Runs : Math.Max(1, value.Quantity);
                }

                ClearAnalysis("Blueprint selection changed. Run analysis to refresh the manufacturing snapshot.");
                OnPropertyChanged(nameof(CanAnalyze));
            }
        }
    }

    public ManufacturingFacilityOption? SelectedFacility
    {
        get => _selectedFacility;
        set
        {
            if (SetProperty(ref _selectedFacility, value))
            {
                ClearAnalysis("Facility selection changed. Run analysis to refresh the manufacturing snapshot.");
                OnPropertyChanged(nameof(CanAnalyze));
            }
        }
    }

    public int UserRuns
    {
        get => _userRuns;
        set => SetProperty(ref _userRuns, value);
    }

    public double ItemMarketCost
    {
        get => _itemMarketCost;
        set => SetProperty(ref _itemMarketCost, value);
    }

    public double RawMaterialsCost
    {
        get => _rawMaterialsCost;
        set => SetProperty(ref _rawMaterialsCost, value);
    }

    public double ComponentMaterialsCost
    {
        get => _componentMaterialsCost;
        set => SetProperty(ref _componentMaterialsCost, value);
    }

    public double AdditionalCosts
    {
        get => _additionalCosts;
        set => SetProperty(ref _additionalCosts, value);
    }

    public double EstimatedItemValue
    {
        get => _estimatedItemValue;
        set => SetProperty(ref _estimatedItemValue, value);
    }

    public bool ApplySalesTax
    {
        get => _applySalesTax;
        set => SetProperty(ref _applySalesTax, value);
    }

    public bool IncludeBrokerFee
    {
        get => _includeBrokerFee;
        set => SetProperty(ref _includeBrokerFee, value);
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
                OnPropertyChanged(nameof(CanAnalyze));
                OnPropertyChanged(nameof(CanRefresh));
            }
        }
    }

    public bool CanAnalyze => !IsBusy && SelectedBlueprint is not null && SelectedFacility is not null;

    public bool CanRefresh => !IsBusy;

    public ManufacturingWorkspaceAnalysisResult? AnalysisResult
    {
        get => _analysisResult;
        private set
        {
            if (SetProperty(ref _analysisResult, value))
            {
                OnPropertyChanged(nameof(HasAnalysisResult));
            }
        }
    }

    public bool HasAnalysisResult => AnalysisResult is not null;

    public async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        await LoadAsync().ConfigureAwait(false);
    }

    public async Task AnalyzeAsync()
    {
        if (!CanAnalyze)
        {
            return;
        }

        try
        {
            IsBusy = true;

            ManufacturingWorkspaceAnalysisRequest request = new(
                SelectedBlueprint!.BlueprintId,
                SelectedFacility!.CharacterId,
                SelectedFacility.ProductionType,
                SelectedFacility.FacilityId,
                Math.Max(1, UserRuns),
                ItemMarketCost,
                RawMaterialsCost,
                ComponentMaterialsCost,
                AdditionalCosts,
                EstimatedItemValue,
                ApplySalesTax,
                IncludeBrokerFee);

            var result = await _manufacturingWorkspaceCommandService
                .AnalyzeAsync(request)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                AnalysisResult = null;
                StatusText = $"Unable to analyze manufacturing: {result.Error.Message}";
                return;
            }

            AnalysisResult = result.Value;
            StatusText = result.Value.StatusText;
        }
        catch (Exception ex)
        {
            AnalysisResult = null;
            StatusText = $"Unable to analyze manufacturing: {ex.Message}";
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

            ManufacturingWorkspaceScreenData screenData = await _manufacturingWorkspaceQueryService
                .GetScreenDataAsync()
                .ConfigureAwait(false);

            Blueprints = screenData.Blueprints;
            Facilities = screenData.Facilities;
            SelectedBlueprint = Blueprints.FirstOrDefault();
            SelectedFacility = Facilities.FirstOrDefault();
            StatusText = screenData.StatusText;
        }
        catch (Exception ex)
        {
            Blueprints = [];
            Facilities = [];
            SelectedBlueprint = null;
            SelectedFacility = null;
            AnalysisResult = null;
            StatusText = $"Unable to load manufacturing workspace: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ClearAnalysis(string nextStatus)
    {
        if (AnalysisResult is null)
        {
            return;
        }

        AnalysisResult = null;
        StatusText = nextStatus;
    }
}