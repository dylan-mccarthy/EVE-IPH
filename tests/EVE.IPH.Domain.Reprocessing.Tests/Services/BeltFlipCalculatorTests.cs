using FluentAssertions;
using EVE.IPH.Domain.Reprocessing.Models;
using EVE.IPH.Domain.Reprocessing.Services;

namespace EVE.IPH.Domain.Reprocessing.Tests.Services;

public sealed class BeltFlipCalculatorTests
{
    private readonly BeltFlipCalculator _sut = new();

    [Fact]
    public void Calculate_WhenUsingRawSaleValues_ReturnsLegacyStyleHoursAndIph()
    {
        BeltFlipInput input = new(
            [
                new BeltFlipLineInput("Veldspar", 1_000, 0.1, 15, 22_000, 350),
                new BeltFlipLineInput("Scordite", 500, 0.15, 18, 11_500, 120),
            ],
            MiningVolumePerHourPerMiner: 3_600,
            MinerCount: 2,
            CalculatePerMiner: false,
            UseCompressedSaleValues: false);

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.MiningVolume.Should().BeApproximately(175d, 0.000001d);
        result.Value.DisplayVolume.Should().BeApproximately(175d, 0.000001d);
        result.Value.HoursToFlip.Should().BeApproximately(175d / 7_200d, 0.000001d);
        result.Value.RawSaleValue.Should().Be(24_000d);
        result.Value.RefinedSaleValue.Should().Be(33_500d);
        result.Value.RefinedIskPerHour.Should().BeGreaterThan(result.Value.RawSaleIskPerHour);
        result.Value.TotalReprocessingUsage.Should().Be(470d);
        result.Value.BetterOutcome.Should().Be(BeltFlipOutcome.Reprocess);
    }

    [Fact]
    public void Calculate_WhenUsingCompressedSaleValues_UsesCompressedBlocksAndRawRemainder()
    {
        BeltFlipInput input = new(
            [
                new BeltFlipLineInput("Plagioclase", 250, 0.35, 100, 20_000, 0, 100, 12_000, 15),
            ],
            MiningVolumePerHourPerMiner: 1_000,
            MinerCount: 1,
            CalculatePerMiner: true,
            UseCompressedSaleValues: true);

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.RawSaleValue.Should().Be(29_000d);
        result.Value.DisplayVolume.Should().BeApproximately(30d + 17.5d, 0.000001d);
        result.Value.HoursToFlipPerMiner.Should().BeApproximately(87.5d / 1_000d, 0.000001d);
        result.Value.BetterOutcome.Should().Be(BeltFlipOutcome.SellRaw);
    }

    [Fact]
    public void Calculate_WhenVolumesAreMissing_ReturnsFailure()
    {
        BeltFlipInput input = new(
            [new BeltFlipLineInput("Veldspar", 10, 0, 1, 1)],
            MiningVolumePerHourPerMiner: 1_000,
            MinerCount: 1,
            CalculatePerMiner: false,
            UseCompressedSaleValues: false);

        var result = _sut.Calculate(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_BELT_VOLUME");
    }
}