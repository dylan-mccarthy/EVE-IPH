namespace server.Models;

public sealed record ManufacturingRequest(
	long BlueprintId,
	int MaterialEfficiency = 0,
	int TimeEfficiency = 0,
	int TotalUnits = 1,
	int RunsPerBlueprint = 1,
	int NumberOfBlueprints = 1,
	int ProductionLines = 1,
	int RegionId = 10000002
);
