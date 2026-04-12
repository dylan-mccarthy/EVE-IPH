using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.UI.Avalonia.Services;

public interface IMiningReprocessingWorkspaceCommandService
{
    Task<Result<MiningReprocessingResult>> CalculateBeltFlipAsync(MiningReprocessingRequest request, CancellationToken cancellationToken = default);
}

public sealed record MiningReprocessingRequest(
    string BeltLinesText,
    double MiningVolumePerHourPerMiner,
    int MinerCount,
    bool CalculatePerMiner,
    bool UseCompressedSaleValues);

public sealed record MiningReprocessingResult(
    BeltFlipResultRow Result,
    string StatusText);

public sealed record BeltFlipResultRow(
    string BetterOutcome,
    double RawSaleValue,
    double RefinedSaleValue,
    double MiningVolume,
    double DisplayVolume,
    double HoursToFlip,
    double HoursToFlipPerMiner,
    double RawSaleIskPerHour,
    double RefinedSaleIskPerHour,
    double TotalReprocessingUsage);