using CommunityToolkit.Mvvm.ComponentModel;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.UI.Avalonia.Services;

namespace EVE.IPH.UI.Avalonia.ViewModels;

public sealed class MiningReprocessingViewModel : ObservableObject
{
    private readonly IMiningReprocessingWorkspaceQueryService _queryService;
    private readonly IMiningReprocessingWorkspaceCommandService _commandService;
    private string _beltLinesText = string.Empty;
    private double _miningVolumePerHourPerMiner;
    private int _minerCount;
    private bool _calculatePerMiner;
    private bool _useCompressedSaleValues;
    private string _statusText = "Loading mining and reprocessing workspace...";
    private BeltFlipResultRow? _result;
    private bool _isBusy;

    public MiningReprocessingViewModel(
        IMiningReprocessingWorkspaceQueryService queryService,
        IMiningReprocessingWorkspaceCommandService commandService)
    {
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
        LoadTask = LoadAsync();
    }

    public Task LoadTask { get; }

    public string BeltLinesText
    {
        get => _beltLinesText;
        set => SetProperty(ref _beltLinesText, value);
    }

    public double MiningVolumePerHourPerMiner
    {
        get => _miningVolumePerHourPerMiner;
        set => SetProperty(ref _miningVolumePerHourPerMiner, value);
    }

    public int MinerCount
    {
        get => _minerCount;
        set => SetProperty(ref _minerCount, value);
    }

    public bool CalculatePerMiner
    {
        get => _calculatePerMiner;
        set => SetProperty(ref _calculatePerMiner, value);
    }

    public bool UseCompressedSaleValues
    {
        get => _useCompressedSaleValues;
        set => SetProperty(ref _useCompressedSaleValues, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public BeltFlipResultRow? Result
    {
        get => _result;
        private set => SetProperty(ref _result, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanRefresh));
                OnPropertyChanged(nameof(CanCalculate));
            }
        }
    }

    public bool CanRefresh => !IsBusy;

    public bool CanCalculate => !IsBusy;

    public async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        await LoadAsync().ConfigureAwait(false);
    }

    public async Task CalculateAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            Result<MiningReprocessingResult> result = await _commandService.CalculateBeltFlipAsync(new MiningReprocessingRequest(
                BeltLinesText,
                MiningVolumePerHourPerMiner,
                MinerCount,
                CalculatePerMiner,
                UseCompressedSaleValues)).ConfigureAwait(false);

            if (result.IsFailure)
            {
                Result = null;
                StatusText = $"Unable to calculate the belt flip: {result.Error.Message}";
                return;
            }

            Result = result.Value.Result;
            StatusText = result.Value.StatusText;
        }
        catch (Exception ex)
        {
            Result = null;
            StatusText = $"Unable to calculate the belt flip: {ex.Message}";
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
            MiningReprocessingScreenData screenData = await _queryService.GetScreenDataAsync().ConfigureAwait(false);
            BeltLinesText = screenData.BeltLinesText;
            MiningVolumePerHourPerMiner = screenData.MiningVolumePerHourPerMiner;
            MinerCount = screenData.MinerCount;
            CalculatePerMiner = screenData.CalculatePerMiner;
            UseCompressedSaleValues = screenData.UseCompressedSaleValues;
            Result = null;
            StatusText = screenData.StatusText;
        }
        catch (Exception ex)
        {
            Result = null;
            StatusText = $"Unable to load the mining and reprocessing workspace: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}