using FluentAssertions;
using EVE.IPH.Domain.Reprocessing.Models;
using EVE.IPH.Domain.Reprocessing.Services;

namespace EVE.IPH.Domain.Reprocessing.Tests.Services;

public sealed class OreConversionOptimizerTests
{
    private readonly OreConversionOptimizer _sut = new();

    [Fact]
    public void Calculate_WhenOneOreCoversAllRequirements_ChoosesMinimumBatches()
    {
        OreConversionInput input = new(
            [
                new OreConversionRequirement("Tritanium", 900),
                new OreConversionRequirement("Pyerite", 180),
            ],
            [
                new OreConversionCandidate(
                    "Veldspar",
                    "Ore",
                    100,
                    12,
                    2.5,
                    [
                        new OreConversionYield("Tritanium", 450),
                        new OreConversionYield("Pyerite", 90),
                    ]),
            ]);

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Selections.Should().ContainSingle();
        result.Value.Selections[0].BatchCount.Should().Be(2);
        result.Value.Selections[0].TotalOreQuantity.Should().Be(200);
        result.Value.TotalObjectiveValue.Should().Be(24);
        result.Value.TotalReprocessingUsage.Should().Be(5d);
        result.Value.ExcessMaterials.Should().ContainSingle(entry => entry.MaterialName == "Tritanium" && entry.ExcessQuantity == 0);
    }

    [Fact]
    public void Calculate_WhenMultipleCandidatesExist_MinimizesObjectiveValue()
    {
        OreConversionInput input = new(
            [new OreConversionRequirement("Tritanium", 1_000)],
            [
                new OreConversionCandidate("Dense Veldspar", "Ore", 100, 11, 1, [new OreConversionYield("Tritanium", 400)]),
                new OreConversionCandidate("Compressed Veldspar", "Ore", 100, 25, 2, [new OreConversionYield("Tritanium", 1_000)]),
            ]);

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Selections.Should().ContainSingle(entry => entry.OreName == "Compressed Veldspar");
        result.Value.TotalObjectiveValue.Should().Be(25d);
    }

    [Fact]
    public void Calculate_WhenSolutionOverproducesMaterials_ReturnsExcess()
    {
        OreConversionInput input = new(
            [new OreConversionRequirement("Isogen", 150)],
            [
                new OreConversionCandidate("Omber", "Ore", 500, 10, 0, [new OreConversionYield("Isogen", 200)]),
            ]);

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExcessMaterials.Should().ContainSingle(entry => entry.MaterialName == "Isogen" && entry.ExcessQuantity == 50);
    }

    [Fact]
    public void Calculate_WhenRequirementCannotBeMet_ReturnsFailure()
    {
        OreConversionInput input = new(
            [new OreConversionRequirement("Megacyte", 10)],
            [new OreConversionCandidate("Veldspar", "Ore", 100, 1, 0, [new OreConversionYield("Tritanium", 400)])]);

        var result = _sut.Calculate(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UNSATISFIABLE_CONVERSION_REQUIREMENT");
    }
}