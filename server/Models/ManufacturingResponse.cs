namespace server.Models;

public sealed record ManufacturingResponse(long BlueprintId, int Runs, decimal TotalCost, decimal TotalProfit, decimal Iph);
