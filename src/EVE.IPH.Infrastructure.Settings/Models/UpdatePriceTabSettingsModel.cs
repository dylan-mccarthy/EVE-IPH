namespace EVE.IPH.Infrastructure.Settings.Models;

public sealed record UpdatePriceTabSettingsModel
{
    public bool AllRawMats { get; init; } = true;
    public bool Gas { get; init; } = true;
    public bool IceProducts { get; init; } = true;
    public bool Minerals { get; init; } = true;
    public bool Planetary { get; init; } = true;
    public bool RawMaterials { get; init; } = true;
    public bool Salvage { get; init; } = true;
    public bool Misc { get; init; } = true;
    public bool Ships { get; init; } = true;
    public bool Modules { get; init; } = true;
    public bool Charges { get; init; } = true;
    public bool Drones { get; init; } = true;
    public bool Rigs { get; init; } = true;
    public bool Subsystems { get; init; } = true;
    public bool FuelBlocks { get; init; } = true;
    public bool AdvancedComponents { get; init; } = true;
    public string SelectedRegion { get; init; } = "The Forge";
    public string SelectedSystem { get; init; } = "Jita";
    public string ItemsCombo { get; init; } = "Min Sell";
    public string RawMatsCombo { get; init; } = "Min Sell";
    public double ItemsPriceModifier { get; init; } = 0.0;
    public double RawPriceModifier { get; init; } = 0.0;
    public int PriceDataSource { get; init; } = 0;
    public bool UsePriceProfile { get; init; } = false;
    public string PpRawPriceType { get; init; } = "Max Buy";
    public string PpRawRegion { get; init; } = "The Forge";
    public string PpRawSystem { get; init; } = "Jita";
    public double PpRawPriceMod { get; init; } = 0.0;
    public string PpItemsPriceType { get; init; } = "Min Sell";
    public string PpItemsRegion { get; init; } = "The Forge";
    public string PpItemsSystem { get; init; } = "Jita";
    public double PpItemsPriceMod { get; init; } = 0.0;
    public int ColumnSort { get; init; } = 1;
    public string ColumnSortType { get; init; } = "Ascending";
}
