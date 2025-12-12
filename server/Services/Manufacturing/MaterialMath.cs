namespace server.Services.Manufacturing;

public static class MaterialMath
{
    public static int CalculateAdjustedQuantity(int baseQuantity, int materialEfficiency, int runs)
    {
        if (baseQuantity <= 0 || runs <= 0)
        {
            return 0;
        }

        // Legacy (VB) behavior:
        // quantity = max(runs, ceil(round(runs * baseQuantity * ((1 - ME/100) * facilityMultiplier), 2)))
        // For now we only apply the ME portion (facilityMultiplier is handled later).
        var me = Math.Clamp(materialEfficiency, 0, 10);
        var modifier = 1m - (me / 100m);
        if (modifier < 0m)
        {
            modifier = 0m;
        }

        var exact = runs * (decimal)baseQuantity * modifier;
        var rounded = Math.Round(exact, 2, MidpointRounding.ToEven);
        var ceiled = (long)Math.Ceiling(rounded);
        var result = Math.Max((long)runs, ceiled);

        if (result <= 0)
        {
            return 0;
        }

        return result > int.MaxValue ? int.MaxValue : (int)result;
    }
}
