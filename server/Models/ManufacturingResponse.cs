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
	decimal ProductValue,
	decimal Profit,
	decimal Iph,
	IReadOnlyList<string> Warnings
);
