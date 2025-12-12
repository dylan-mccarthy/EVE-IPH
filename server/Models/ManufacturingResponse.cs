namespace server.Models;

public sealed record ManufacturingLineItem(
	int TypeId,
	string TypeName,
	int Quantity,
	decimal UnitPrice,
	decimal TotalCost,
	bool MissingPrice
);

public sealed record ManufacturingResponse(
	long BlueprintId,
	string BlueprintName,
	int RegionId,
	int MaterialEfficiency,
	int TimeEfficiency,
	int TotalUnits,
	IReadOnlyList<ManufacturingLineItem> ComponentMaterials,
	IReadOnlyList<ManufacturingLineItem> RawMaterials,
	decimal ComponentTotalCost,
	decimal RawTotalCost,
	decimal? BuildBuyTotalCost,
	decimal ProductValue,
	decimal SalesTax,
	decimal BrokerFee,
	decimal JobInstallationCost,
	decimal TotalEiv,
	decimal TotalTimeSeconds,
	decimal ProfitComponents,
	decimal ProfitRaw,
	decimal? ProfitBuildBuy,
	decimal Profit,
	decimal IphComponents,
	decimal IphRaw,
	decimal? IphBuildBuy,
	decimal Iph,
	decimal? ExcessSellValueNet,
	IReadOnlyList<string> Warnings
);
