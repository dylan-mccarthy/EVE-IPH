using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Reprocessing.Models;
using Google.OrTools.LinearSolver;

namespace EVE.IPH.Domain.Reprocessing.Services;

public sealed class OreConversionOptimizer
{
    public Result<OreConversionResult> Calculate(OreConversionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Requirements.Count == 0)
        {
            return Result<OreConversionResult>.Failure("MISSING_CONVERSION_REQUIREMENTS", "At least one conversion requirement is required.");
        }

        if (input.Candidates.Count == 0)
        {
            return Result<OreConversionResult>.Failure("MISSING_CONVERSION_CANDIDATES", "At least one ore conversion candidate is required.");
        }

        Dictionary<string, double> requirements = new(StringComparer.OrdinalIgnoreCase);
        foreach (OreConversionRequirement requirement in input.Requirements)
        {
            if (string.IsNullOrWhiteSpace(requirement.MaterialName) || requirement.RequiredQuantity <= 0)
            {
                return Result<OreConversionResult>.Failure("INVALID_CONVERSION_REQUIREMENT", "Requirement names must be set and quantities must be greater than zero.");
            }

            requirements[requirement.MaterialName] = requirements.TryGetValue(requirement.MaterialName, out double existing)
                ? existing + requirement.RequiredQuantity
                : requirement.RequiredQuantity;
        }

        foreach (OreConversionCandidate candidate in input.Candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate.OreName) || string.IsNullOrWhiteSpace(candidate.GroupName))
            {
                return Result<OreConversionResult>.Failure("INVALID_CONVERSION_CANDIDATE", "Candidate ore names and groups must be set.");
            }

            if (candidate.UnitsPerBatch <= 0 || candidate.ObjectiveValuePerBatch < 0 || candidate.ReprocessingUsagePerBatch < 0 || candidate.Yields.Count == 0)
            {
                return Result<OreConversionResult>.Failure("INVALID_CONVERSION_CANDIDATE", "Candidate ore values must be non-negative, units per batch must be greater than zero, and yields must be present.");
            }

            foreach (OreConversionYield yield in candidate.Yields)
            {
                if (string.IsNullOrWhiteSpace(yield.MaterialName) || yield.QuantityPerBatch < 0)
                {
                    return Result<OreConversionResult>.Failure("INVALID_CONVERSION_YIELD", "Yield names must be set and yield quantities must be zero or greater.");
                }
            }
        }

        foreach ((string materialName, _) in requirements)
        {
            bool covered = input.Candidates.Any(candidate => candidate.Yields.Any(yield => yield.MaterialName.Equals(materialName, StringComparison.OrdinalIgnoreCase) && yield.QuantityPerBatch > 0));
            if (!covered)
            {
                return Result<OreConversionResult>.Failure("UNSATISFIABLE_CONVERSION_REQUIREMENT", $"No ore candidate can produce required material '{materialName}'.");
            }
        }

        Solver? solver = Solver.CreateSolver("CBC_MIXED_INTEGER_PROGRAMMING");
        if (solver is null)
        {
            return Result<OreConversionResult>.Failure("CONVERSION_SOLVER_UNAVAILABLE", "The ore conversion optimizer is unavailable in the current environment.");
        }

        List<Variable> variables = [];
        for (int index = 0; index < input.Candidates.Count; index++)
        {
            variables.Add(solver.MakeIntVar(0, double.PositiveInfinity, $"ore_{index}"));
        }

        foreach ((string materialName, double requiredQuantity) in requirements)
        {
            Constraint constraint = solver.MakeConstraint(requiredQuantity, double.PositiveInfinity, materialName);
            for (int index = 0; index < input.Candidates.Count; index++)
            {
                double producedQuantity = input.Candidates[index].Yields
                    .Where(yield => yield.MaterialName.Equals(materialName, StringComparison.OrdinalIgnoreCase))
                    .Sum(yield => yield.QuantityPerBatch);

                if (producedQuantity > 0)
                {
                    constraint.SetCoefficient(variables[index], producedQuantity);
                }
            }
        }

        Objective objective = solver.Objective();
        for (int index = 0; index < input.Candidates.Count; index++)
        {
            objective.SetCoefficient(variables[index], input.Candidates[index].ObjectiveValuePerBatch);
        }

        objective.SetMinimization();

        Solver.ResultStatus resultStatus = solver.Solve();
        if (resultStatus is not Solver.ResultStatus.OPTIMAL and not Solver.ResultStatus.FEASIBLE)
        {
            return Result<OreConversionResult>.Failure("NO_CONVERSION_SOLUTION", "No feasible ore conversion solution was found for the requested materials.");
        }

        List<OreConversionSelection> selections = [];
        Dictionary<string, double> producedByMaterial = new(StringComparer.OrdinalIgnoreCase);
        double totalObjectiveValue = 0d;
        double totalReprocessingUsage = 0d;

        for (int index = 0; index < input.Candidates.Count; index++)
        {
            long batchCount = (long)Math.Round(variables[index].SolutionValue());
            if (batchCount <= 0)
            {
                continue;
            }

            OreConversionCandidate candidate = input.Candidates[index];
            double candidateObjectiveValue = candidate.ObjectiveValuePerBatch * batchCount;
            double candidateReprocessingUsage = candidate.ReprocessingUsagePerBatch * batchCount;

            selections.Add(new OreConversionSelection(
                candidate.OreName,
                candidate.GroupName,
                batchCount,
                batchCount * candidate.UnitsPerBatch,
                candidateObjectiveValue,
                candidateReprocessingUsage));

            totalObjectiveValue += candidateObjectiveValue;
            totalReprocessingUsage += candidateReprocessingUsage;

            foreach (OreConversionYield yield in candidate.Yields)
            {
                producedByMaterial[yield.MaterialName] = producedByMaterial.TryGetValue(yield.MaterialName, out double existing)
                    ? existing + (yield.QuantityPerBatch * batchCount)
                    : yield.QuantityPerBatch * batchCount;
            }
        }

        List<OreConversionExcessMaterial> excessMaterials = [];
        foreach ((string materialName, double requiredQuantity) in requirements.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
        {
            double producedQuantity = producedByMaterial.TryGetValue(materialName, out double produced) ? produced : 0d;
            excessMaterials.Add(new OreConversionExcessMaterial(
                materialName,
                requiredQuantity,
                producedQuantity,
                producedQuantity - requiredQuantity));
        }

        return Result<OreConversionResult>.Success(new OreConversionResult(
            selections,
            excessMaterials,
            totalObjectiveValue,
            totalReprocessingUsage));
    }
}