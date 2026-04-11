namespace EVE.IPH.Infrastructure.Settings.Models;

public sealed record MiningTabSettingsModel
{
    public string OreType { get; init; } = "Ore";
    public bool CheckHighYieldOres { get; init; } = true;
    public bool CheckHighSecOres { get; init; } = true;
    public bool CheckLowSecOres { get; init; } = false;
    public bool CheckNullSecOres { get; init; } = false;
}
