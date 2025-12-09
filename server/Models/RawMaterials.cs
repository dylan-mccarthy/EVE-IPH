namespace server.Models;

/// <summary>
/// Raw material breakdown for a blueprint, recursively calculating component costs
/// </summary>
public record RawMaterialsRequest(
    long BlueprintId,
    int MaterialEfficiency = 0,
    int Runs = 1
);

/// <summary>
/// Response containing both component and raw material breakdowns
/// </summary>
public record RawMaterialsResponse(
    long BlueprintId,
    string BlueprintName,
    List<MaterialBreakdown> ComponentMaterials,
    List<MaterialBreakdown> RawMaterials
);

/// <summary>
/// Material breakdown with pricing information
/// </summary>
public record MaterialBreakdown(
    int TypeId,
    string TypeName,
    int Quantity,
    bool IsManufacturable,
    int? BlueprintId = null
);
