namespace EVE.IPH.Infrastructure.Settings.Models;

public sealed record ShoppingListSettingsModel
{
    public string DataExportFormat { get; init; } = "Default";
    public bool AlwaysOnTop { get; init; } = false;
    public bool UpdateAssetsWhenUsed { get; init; } = false;
    public bool Fees { get; init; } = false;
    public int CalcBuyBuyOrder { get; init; } = 0;
    public bool Usage { get; init; } = false;
    public bool ReloadBpsFromFile { get; init; } = false;
}
