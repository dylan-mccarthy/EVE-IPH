using EVE.IPH.Domain.Assets.Models;
using EVE.IPH.Domain.Assets.Services;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class AssetsScreenService : IAssetsScreenService
{
    private readonly IPhase11SampleDataProvider _sampleDataProvider;
    private readonly IAssetSnapshotHydrator _assetSnapshotHydrator;

    public AssetsScreenService(
        IPhase11SampleDataProvider sampleDataProvider,
        IAssetSnapshotHydrator assetSnapshotHydrator)
    {
        _sampleDataProvider = sampleDataProvider ?? throw new ArgumentNullException(nameof(sampleDataProvider));
        _assetSnapshotHydrator = assetSnapshotHydrator ?? throw new ArgumentNullException(nameof(assetSnapshotHydrator));
    }

    public IReadOnlyList<HydratedAsset> GetHydratedAssets() =>
        _assetSnapshotHydrator.Hydrate(
            _sampleDataProvider.GetAssetRecords(),
            _sampleDataProvider.GetAssetTypeMetadata(),
            _sampleDataProvider.GetAssetLocationMetadata());
}