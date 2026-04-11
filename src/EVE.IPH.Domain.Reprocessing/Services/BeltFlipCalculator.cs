using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Reprocessing.Models;

namespace EVE.IPH.Domain.Reprocessing.Services;

public sealed class BeltFlipCalculator
{
    public Result<BeltFlipResult> Calculate(BeltFlipInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Lines.Count == 0)
        {
            return Result<BeltFlipResult>.Failure("MISSING_BELT_LINES", "At least one belt line is required.");
        }

        if (input.MiningVolumePerHourPerMiner <= 0 || input.MinerCount <= 0)
        {
            return Result<BeltFlipResult>.Failure("INVALID_MINING_RATE", "Mining rate and miner count must be greater than zero.");
        }

        double rawSaleValue = 0d;
        double refinedSaleValue = 0d;
        double miningVolume = 0d;
        double displayVolume = 0d;
        double totalReprocessingUsage = 0d;

        foreach (BeltFlipLineInput line in input.Lines)
        {
            if (string.IsNullOrWhiteSpace(line.ItemName) || line.Quantity < 0 || line.MiningVolumePerUnit < 0 || line.RawUnitSaleValue < 0 || line.RefinedSaleValue < 0 || line.ReprocessingUsage < 0)
            {
                return Result<BeltFlipResult>.Failure("INVALID_BELT_LINE", "Belt lines must have valid names and non-negative quantities, prices, volumes, and usage values.");
            }

            if (line.Quantity == 0)
            {
                continue;
            }

            miningVolume += line.Quantity * line.MiningVolumePerUnit;
            refinedSaleValue += line.RefinedSaleValue;
            totalReprocessingUsage += line.ReprocessingUsage;

            if (input.UseCompressedSaleValues && line.CompressedBatchSaleValue.HasValue && line.CompressedBatchVolume.HasValue)
            {
                if (line.CompressionBatchSize <= 0)
                {
                    return Result<BeltFlipResult>.Failure("INVALID_COMPRESSION_BATCH", "Compression batch size must be greater than zero when compressed sale values are used.");
                }

                long compressedBlocks = line.Quantity / line.CompressionBatchSize;
                long remainderUnits = line.Quantity - (compressedBlocks * line.CompressionBatchSize);

                rawSaleValue += (compressedBlocks * line.CompressedBatchSaleValue.Value) + (remainderUnits * line.RawUnitSaleValue);
                displayVolume += (compressedBlocks * line.CompressedBatchVolume.Value) + (remainderUnits * line.MiningVolumePerUnit);
            }
            else
            {
                rawSaleValue += line.Quantity * line.RawUnitSaleValue;
                displayVolume += line.Quantity * line.MiningVolumePerUnit;
            }
        }

        if (miningVolume == 0d)
        {
            return Result<BeltFlipResult>.Failure("INVALID_BELT_VOLUME", "At least one belt line must contribute mining volume.");
        }

        double hoursToFlip = miningVolume / (input.MiningVolumePerHourPerMiner * input.MinerCount);
        double hoursToFlipPerMiner = miningVolume / input.MiningVolumePerHourPerMiner;
        double divisor = input.CalculatePerMiner ? hoursToFlipPerMiner : hoursToFlip;

        double rawSaleIskPerHour = rawSaleValue / divisor;
        double refinedSaleIskPerHour = refinedSaleValue / divisor;

        BeltFlipOutcome betterOutcome = refinedSaleValue.CompareTo(rawSaleValue) switch
        {
            > 0 => BeltFlipOutcome.Reprocess,
            < 0 => BeltFlipOutcome.SellRaw,
            _ => BeltFlipOutcome.EqualValue,
        };

        return Result<BeltFlipResult>.Success(new BeltFlipResult(
            rawSaleValue,
            refinedSaleValue,
            miningVolume,
            displayVolume,
            hoursToFlip,
            hoursToFlipPerMiner,
            rawSaleIskPerHour,
            refinedSaleIskPerHour,
            totalReprocessingUsage,
            betterOutcome));
    }
}