using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.UI.Avalonia.Services;

public interface IManufacturingWorkspaceCommandService
{
    Task<Result<ManufacturingWorkspaceAnalysisResult>> AnalyzeAsync(
        ManufacturingWorkspaceAnalysisRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record ManufacturingWorkspaceAnalysisRequest(
    BlueprintId BlueprintId,
    CharacterId FacilityCharacterId,
    FacilityProductionType ProductionType,
    long FacilityId,
    int UserRuns,
    double ItemMarketCost,
    double RawMaterialsCost,
    double ComponentMaterialsCost,
    double AdditionalCosts,
    double EstimatedItemValue,
    bool ApplySalesTax,
    bool IncludeBrokerFee);

public sealed record ManufacturingWorkspaceAnalysisResult(
    string BlueprintName,
    string ProductName,
    string FacilityName,
    string OperatorName,
    bool CanBuild,
    bool CheaperToBuild,
    bool BuildItem,
    int UserRuns,
    double TotalRawCost,
    double TotalComponentCost,
    double TotalRawProfit,
    double TotalComponentProfit,
    double TotalIskPerHourRaw,
    double TotalIskPerHourComponent,
    double TotalUsage,
    double TotalProductionTimeSeconds,
    double BlueprintProductionTimeSeconds,
    string StatusText);