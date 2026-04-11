using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Manufacturing.Services;

namespace EVE.IPH.Domain.Manufacturing.Tests.Services;

public sealed class CorporationBlueprintServiceTests
{
    [Fact]
    public async Task RefreshAsync_ReplacesStoredBlueprintsForCorporation()
    {
        FakeOwnedBlueprintRepository ownedBlueprintRepository = new();
        FakeOwnedBlueprintDataSource ownedBlueprintDataSource = new();
        CorporationId corporationId = new(98000001);
        CharacterId characterId = new(90000001);

        ownedBlueprintDataSource.Blueprints = [
            new OwnedBlueprintData(corporationId.Value, new ItemId(7000001), 60015068, new BlueprintId(28607), "Orca Blueprint", 1, 10, 20, -1, 1, true, true),
        ];

        CorporationBlueprintService sut = new(ownedBlueprintRepository, ownedBlueprintDataSource);

        var result = await sut.RefreshAsync(corporationId, characterId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].BlueprintName.Should().Be("Orca Blueprint");
        ownedBlueprintRepository.LastUserId.Should().Be(corporationId.Value);
        ownedBlueprintRepository.LastReplacedBlueprints.Should().ContainSingle();
        ownedBlueprintRepository.LastReplacedBlueprints[0].BlueprintId.Value.Should().Be(28607);
    }

    private sealed class FakeOwnedBlueprintRepository : IOwnedBlueprintRepository
    {
        public long LastUserId { get; private set; }

        public IReadOnlyList<OwnedBlueprintRecord> LastReplacedBlueprints { get; private set; } = [];

        public Task<Result<bool>> DeleteAsync(long userId, BlueprintId blueprintId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<bool>.Success(true));

        public Task<Result<bool>> DeleteByUserAsync(long userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<bool>.Success(true));

        public Task<Result<IReadOnlyList<OwnedBlueprintRecord>>> GetByUserAsync(long userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<IReadOnlyList<OwnedBlueprintRecord>>.Success([]));

        public Task<Result<IReadOnlyList<OwnedBlueprintRecord>>> GetByUsersAsync(IReadOnlyList<long> userIds, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<IReadOnlyList<OwnedBlueprintRecord>>.Success([]));

        public Task<Result<IReadOnlyList<OwnedBlueprintRecord>>> ReplaceAsync(long userId, IReadOnlyList<OwnedBlueprintRecord> blueprints, CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            LastReplacedBlueprints = blueprints;
            return Task.FromResult(Result<IReadOnlyList<OwnedBlueprintRecord>>.Success(blueprints));
        }

        public Task<Result<OwnedBlueprintRecord>> UpsertAsync(OwnedBlueprintRecord record, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<OwnedBlueprintRecord>.Success(record));
    }

    private sealed class FakeOwnedBlueprintDataSource : IOwnedBlueprintDataSource
    {
        public IReadOnlyList<OwnedBlueprintData> Blueprints { get; set; } = [];

        public Task<Result<IReadOnlyList<OwnedBlueprintData>>> GetCorporationBlueprintsAsync(CorporationId corporationId, CharacterId authenticatedCharacterId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<IReadOnlyList<OwnedBlueprintData>>.Success(Blueprints));
    }
}