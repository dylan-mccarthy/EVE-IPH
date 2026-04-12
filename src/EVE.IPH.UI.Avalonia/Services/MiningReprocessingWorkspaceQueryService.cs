namespace EVE.IPH.UI.Avalonia.Services;

public sealed class MiningReprocessingWorkspaceQueryService : IMiningReprocessingWorkspaceQueryService
{
    private const string DefaultBeltLines = "Veldspar|1000|0.1|15|22000|350\nScordite|500|0.15|18|11500|120";

    public Task<MiningReprocessingScreenData> GetScreenDataAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new MiningReprocessingScreenData(
            DefaultBeltLines,
            3600d,
            2,
            false,
            false,
            "Enter belt lines as Name|Quantity|MiningVolumePerUnit|RawUnitSaleValue|RefinedSaleValue|OptionalReprocessingUsage|OptionalCompressionBatchSize|OptionalCompressedBatchSaleValue|OptionalCompressedBatchVolume. This first mining/reprocessing slice is a belt-flip workspace over the extracted reprocessing domain."));
    }
}