namespace EVE.IPH.UI.Avalonia.Services;

public interface IMiningReprocessingWorkspaceQueryService
{
    Task<MiningReprocessingScreenData> GetScreenDataAsync(CancellationToken cancellationToken = default);
}

public sealed record MiningReprocessingScreenData(
    string BeltLinesText,
    double MiningVolumePerHourPerMiner,
    int MinerCount,
    bool CalculatePerMiner,
    bool UseCompressedSaleValues,
    string StatusText);