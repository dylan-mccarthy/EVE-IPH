namespace server.Models;

public record BlueprintDetails(
    long BlueprintId,
    string BlueprintName,
    string ItemGroup,
    string ItemCategory,
    IReadOnlyList<BlueprintActivity> Activities
);

public record BlueprintActivity(
    int ActivityId,
    string ActivityName,
    long ProductId,
    string ProductName,
    int ProductQuantity,
    IReadOnlyList<BlueprintMaterial> Materials,
    IReadOnlyList<BlueprintProduct> Products
);

public record BlueprintMaterial(
    long MaterialId,
    string MaterialName,
    string MaterialGroup,
    string MaterialCategory,
    int Quantity,
    double Volume,
    bool Consume
);

public record BlueprintProduct(
    long ProductId,
    string ProductName,
    int Quantity,
    double Probability
);
