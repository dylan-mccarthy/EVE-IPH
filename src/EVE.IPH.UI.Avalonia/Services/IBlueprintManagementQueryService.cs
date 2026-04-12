using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.UI.Avalonia.Services;

public interface IBlueprintManagementQueryService
{
    Task<BlueprintManagementScreenData> GetScreenDataAsync(CancellationToken cancellationToken = default);
}

public sealed record BlueprintManagementScreenData(
    IReadOnlyList<BlueprintManagementRow> Blueprints,
    IReadOnlyList<BlueprintOwnerFilterOption> OwnerOptions,
    string StatusText);

public sealed record BlueprintOwnerFilterOption(long? OwnerId, string DisplayName);

public sealed record BlueprintManagementRow(
    long OwnerId,
    string OwnerName,
    bool IsCorporationOwner,
    ItemId ItemId,
    long LocationId,
    BlueprintId BlueprintId,
    string BlueprintName,
    int Quantity,
    int Me,
    int Te,
    int Runs,
    int BpType,
    bool Owned,
    bool Scanned)
{
    public string OwnerKindText => IsCorporationOwner ? "Corporation" : "Character";

    public string BlueprintStateText => Runs < 0 ? "Original" : $"{Runs} runs";
}