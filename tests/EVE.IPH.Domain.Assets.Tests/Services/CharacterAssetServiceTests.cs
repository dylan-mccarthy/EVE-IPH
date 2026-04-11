using EVE.IPH.Domain.Assets.Models;
using EVE.IPH.Domain.Assets.Services;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using NSubstitute;

namespace EVE.IPH.Domain.Assets.Tests.Services;

public sealed class CharacterAssetServiceTests
{
    [Fact]
    public async Task GetAsync_LoadsStoredAssets()
    {
        CharacterId characterId = new(90000001);
        IAssetRepository repository = Substitute.For<IAssetRepository>();
        IAssetDataSource dataSource = Substitute.For<IAssetDataSource>();

        repository.GetByOwnerIdAsync(characterId.Value, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<StoredAssetRecord>>.Success([
                new StoredAssetRecord(characterId.Value, 70000001, 60003760, new TypeId(34), 5000, 5, false, false, "Tritanium Stack"),
            ]));

        CharacterAssetService service = new(repository, dataSource);

        Result<IReadOnlyList<AssetRecord>> result = await service.GetAsync(characterId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].OwnerId.Should().Be(characterId.Value);
        result.Value[0].TypeId.Value.Should().Be(34);
    }

    [Fact]
    public async Task RefreshAsync_ReplacesStoredAssetsWithLatestData()
    {
        CharacterId characterId = new(90000002);
        IAssetRepository repository = Substitute.For<IAssetRepository>();
        IAssetDataSource dataSource = Substitute.For<IAssetDataSource>();

        dataSource.GetCharacterAssetsAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<AssetData>>.Success([
                new AssetData(characterId.Value, 70000002, 60003760, new TypeId(35), 1234, 4, true, true, "Ammo Copy"),
            ]));
        repository.ReplaceAsync(Arg.Any<long>(), Arg.Any<IReadOnlyList<StoredAssetRecord>>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<IReadOnlyList<StoredAssetRecord>>.Success(call.ArgAt<IReadOnlyList<StoredAssetRecord>>(1)));

        CharacterAssetService service = new(repository, dataSource);

        Result<IReadOnlyList<AssetRecord>> result = await service.RefreshAsync(characterId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].IsBlueprintCopy.Should().BeTrue();
        await repository.Received(1).ReplaceAsync(
            Arg.Is<long>(id => id == characterId.Value),
            Arg.Is<IReadOnlyList<StoredAssetRecord>>(assets => assets.Count == 1 && assets[0].OwnerId == characterId.Value && assets[0].ItemId == 70000002),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_DataSourceFailure_ReturnsFailure()
    {
        CharacterId characterId = new(90000003);
        IAssetRepository repository = Substitute.For<IAssetRepository>();
        IAssetDataSource dataSource = Substitute.For<IAssetDataSource>();

        dataSource.GetCharacterAssetsAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<AssetData>>.Failure("asset.fetch.failed", "Unable to fetch assets."));

        CharacterAssetService service = new(repository, dataSource);

        Result<IReadOnlyList<AssetRecord>> result = await service.RefreshAsync(characterId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("asset.fetch.failed");
        await repository.DidNotReceiveWithAnyArgs().ReplaceAsync(default, default!, default);
    }
}