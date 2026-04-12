using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Reprocessing.Models;
using EVE.IPH.Domain.Reprocessing.Services;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class MiningReprocessingWorkspaceCommandService(BeltFlipCalculator beltFlipCalculator) : IMiningReprocessingWorkspaceCommandService
{
    private readonly BeltFlipCalculator _beltFlipCalculator = beltFlipCalculator ?? throw new ArgumentNullException(nameof(beltFlipCalculator));

    public Task<Result<MiningReprocessingResult>> CalculateBeltFlipAsync(MiningReprocessingRequest request, CancellationToken cancellationToken = default)
    {
        if (request.MiningVolumePerHourPerMiner <= 0)
        {
            return Task.FromResult(Result<MiningReprocessingResult>.Failure("INVALID_MINING_RATE", "Mining volume per hour per miner must be greater than zero."));
        }

        if (request.MinerCount <= 0)
        {
            return Task.FromResult(Result<MiningReprocessingResult>.Failure("INVALID_MINER_COUNT", "Miner count must be greater than zero."));
        }

        Result<IReadOnlyList<BeltFlipLineInput>> parseResult = ParseLines(request.BeltLinesText);
        if (parseResult.IsFailure)
        {
            return Task.FromResult(Result<MiningReprocessingResult>.Failure(parseResult.Error));
        }

        Result<BeltFlipResult> calculationResult = _beltFlipCalculator.Calculate(new BeltFlipInput(
            parseResult.Value,
            request.MiningVolumePerHourPerMiner,
            request.MinerCount,
            request.CalculatePerMiner,
            request.UseCompressedSaleValues));

        if (calculationResult.IsFailure)
        {
            return Task.FromResult(Result<MiningReprocessingResult>.Failure(calculationResult.Error));
        }

        BeltFlipResult result = calculationResult.Value;
        return Task.FromResult(Result<MiningReprocessingResult>.Success(new MiningReprocessingResult(
            new BeltFlipResultRow(
                result.BetterOutcome.ToString(),
                result.RawSaleValue,
                result.RefinedSaleValue,
                result.MiningVolume,
                result.DisplayVolume,
                result.HoursToFlip,
                result.HoursToFlipPerMiner,
                result.RawSaleIskPerHour,
                result.RefinedIskPerHour,
                result.TotalReprocessingUsage),
            $"Calculated a belt-flip comparison for {parseResult.Value.Count} belt line{(parseResult.Value.Count == 1 ? string.Empty : "s")}. Better outcome: {result.BetterOutcome}.")));
    }

    private static Result<IReadOnlyList<BeltFlipLineInput>> ParseLines(string beltLinesText)
    {
        if (string.IsNullOrWhiteSpace(beltLinesText))
        {
            return Result<IReadOnlyList<BeltFlipLineInput>>.Failure("MISSING_BELT_LINES", "Enter at least one belt line to calculate the belt flip.");
        }

        List<BeltFlipLineInput> lines = [];
        string[] rawLines = beltLinesText
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string rawLine in rawLines)
        {
            string[] parts = rawLine.Split('|', StringSplitOptions.TrimEntries);
            if (parts.Length < 5)
            {
                return Result<IReadOnlyList<BeltFlipLineInput>>.Failure(
                    "INVALID_BELT_LINE_FORMAT",
                    $"Each belt line must include at least 5 pipe-delimited fields. Invalid line: '{rawLine}'.");
            }

            if (!long.TryParse(parts[1], out long quantity)
                || !double.TryParse(parts[2], out double volumePerUnit)
                || !double.TryParse(parts[3], out double rawUnitSaleValue)
                || !double.TryParse(parts[4], out double refinedSaleValue))
            {
                return Result<IReadOnlyList<BeltFlipLineInput>>.Failure(
                    "INVALID_BELT_LINE_VALUES",
                    $"Each belt line must contain numeric quantity, mining volume, raw sale value, and refined sale value fields. Invalid line: '{rawLine}'.");
            }

            double reprocessingUsage = 0d;
            int compressionBatchSize = 100;
            double? compressedBatchSaleValue = null;
            double? compressedBatchVolume = null;

            if (parts.Length >= 6 && !string.IsNullOrWhiteSpace(parts[5]) && !double.TryParse(parts[5], out reprocessingUsage))
            {
                return Result<IReadOnlyList<BeltFlipLineInput>>.Failure("INVALID_REPROCESSING_USAGE", $"Reprocessing usage must be numeric. Invalid line: '{rawLine}'.");
            }

            if (parts.Length >= 7 && !string.IsNullOrWhiteSpace(parts[6]) && !int.TryParse(parts[6], out compressionBatchSize))
            {
                return Result<IReadOnlyList<BeltFlipLineInput>>.Failure("INVALID_COMPRESSION_BATCH", $"Compression batch size must be numeric. Invalid line: '{rawLine}'.");
            }

            if (parts.Length >= 8 && !string.IsNullOrWhiteSpace(parts[7]))
            {
                if (!double.TryParse(parts[7], out double parsedCompressedSaleValue))
                {
                    return Result<IReadOnlyList<BeltFlipLineInput>>.Failure("INVALID_COMPRESSED_SALE_VALUE", $"Compressed batch sale value must be numeric. Invalid line: '{rawLine}'.");
                }

                compressedBatchSaleValue = parsedCompressedSaleValue;
            }

            if (parts.Length >= 9 && !string.IsNullOrWhiteSpace(parts[8]))
            {
                if (!double.TryParse(parts[8], out double parsedCompressedBatchVolume))
                {
                    return Result<IReadOnlyList<BeltFlipLineInput>>.Failure("INVALID_COMPRESSED_VOLUME", $"Compressed batch volume must be numeric. Invalid line: '{rawLine}'.");
                }

                compressedBatchVolume = parsedCompressedBatchVolume;
            }

            lines.Add(new BeltFlipLineInput(
                parts[0],
                quantity,
                volumePerUnit,
                rawUnitSaleValue,
                refinedSaleValue,
                reprocessingUsage,
                compressionBatchSize,
                compressedBatchSaleValue,
                compressedBatchVolume));
        }

        return Result<IReadOnlyList<BeltFlipLineInput>>.Success(lines);
    }
}