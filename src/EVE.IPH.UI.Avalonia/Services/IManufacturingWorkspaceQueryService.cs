using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.UI.Avalonia.Services;

public interface IManufacturingWorkspaceQueryService
{
    Task<ManufacturingWorkspaceScreenData> GetScreenDataAsync(CancellationToken cancellationToken = default);
}

public sealed record ManufacturingWorkspaceScreenData(
    IReadOnlyList<ManufacturingBlueprintOption> Blueprints,
    IReadOnlyList<ManufacturingFacilityOption> Facilities,
    string StatusText);

public sealed record ManufacturingBlueprintOption(
    long OwnerId,
    string OwnerName,
    bool IsCorporationOwner,
    BlueprintId BlueprintId,
    string BlueprintName,
    int Me,
    int Te,
    int Runs,
    int Quantity,
    bool Owned)
{
    public string OwnerKindText => IsCorporationOwner ? "Corporation" : "Character";
}

public sealed record ManufacturingFacilityOption(
    CharacterId CharacterId,
    FacilityProductionType ProductionType,
    long FacilityId,
    string FacilityName,
    string CharacterName,
    string RegionName,
    string SolarSystemName,
    double CostIndex,
    double TaxRate)
{
    public string DisplayName => $"{FacilityName} ({CharacterName})";

    public string DetailText => $"{SolarSystemName}, {RegionName}  Cost Index {CostIndex:P2}  Tax {TaxRate:P2}";
}