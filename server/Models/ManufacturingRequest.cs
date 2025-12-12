namespace server.Models;

public sealed record ManufacturingRequest(
	long BlueprintId,
	int MaterialEfficiency = 0,
	int TimeEfficiency = 0,
	int TotalUnits = 1,
	int RunsPerBlueprint = 1,
	int NumberOfBlueprints = 1,
	int ProductionLines = 1,
	decimal FacilityMaterialMultiplier = 1m,
	decimal FacilityTimeMultiplier = 1m,
	decimal SalesTaxRate = 0m,
	decimal BrokerFeeRate = 0m,
	decimal JobInstallationCost = 0m,
	string MaterialMarketMode = "Buy",
	string ProductMarketMode = "SellOrder",
	string ProfitCostBasis = "Components",
	bool SellExcessItems = false,
	decimal SystemCostIndex = 0m,
	decimal FacilityCostMultiplier = 1m,
	decimal FacilityTaxRate = 0m,
	decimal SccSurchargeRate = 0m,
	int RegionId = 10000002
);
