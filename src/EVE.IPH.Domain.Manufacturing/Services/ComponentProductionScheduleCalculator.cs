using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Manufacturing.Models;

namespace EVE.IPH.Domain.Manufacturing.Services;

public sealed class ComponentProductionScheduleCalculator
{
    public Result<ComponentProductionScheduleResult> Calculate(ComponentProductionScheduleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.ProductionTimesSeconds);

        if (input.AvailableProductionLines <= 0)
        {
            return Result<ComponentProductionScheduleResult>.Failure("INVALID_PRODUCTION_LINES", "Available production lines must be greater than zero.");
        }

        if (input.ProductionTimesSeconds.Any(time => time < 0))
        {
            return Result<ComponentProductionScheduleResult>.Failure("INVALID_COMPONENT_TIME", "Component production times must be zero or greater.");
        }

        double totalComponentProductionTimeSeconds = CalculateSessionTime(input.ProductionTimesSeconds, input.AvailableProductionLines);

        return Result<ComponentProductionScheduleResult>.Success(new ComponentProductionScheduleResult(totalComponentProductionTimeSeconds));
    }

    private static double CalculateSessionTime(IReadOnlyList<double> productionTimesSeconds, int availableProductionLines)
    {
        if (productionTimesSeconds.Count == 0)
        {
            return 0;
        }

        if (availableProductionLines == 1)
        {
            return productionTimesSeconds.Sum();
        }

        if (productionTimesSeconds.Count <= availableProductionLines)
        {
            return productionTimesSeconds.Max();
        }

        List<double> sortedTimes = [.. productionTimesSeconds.OrderBy(time => time)];
        double maxComponentTime = sortedTimes[^1];
        double remainingTimeSum = sortedTimes.Take(sortedTimes.Count - 1).Sum();

        if (maxComponentTime > remainingTimeSum)
        {
            return maxComponentTime;
        }

        remainingTimeSum = 0;
        double jobTimeSum = 0;
        int jobCount = 1;
        int index = sortedTimes.Count - 2;

        while (index >= 1)
        {
            double nextJobTime = sortedTimes[index];

            if (jobTimeSum + nextJobTime > maxComponentTime)
            {
                if (jobCount < availableProductionLines)
                {
                    jobCount++;
                    remainingTimeSum += jobTimeSum;
                    jobTimeSum = 0;
                    continue;
                }

                remainingTimeSum += jobTimeSum;
                break;
            }

            jobTimeSum += nextJobTime;
            index--;
        }

        if (jobCount == availableProductionLines)
        {
            int remainingCount = Math.Max(index, 0);
            List<double> remainingTimes = sortedTimes.Take(remainingCount).ToList();
            remainingTimeSum += CalculateSessionTime(remainingTimes, availableProductionLines);
        }

        return maxComponentTime + remainingTimeSum;
    }
}