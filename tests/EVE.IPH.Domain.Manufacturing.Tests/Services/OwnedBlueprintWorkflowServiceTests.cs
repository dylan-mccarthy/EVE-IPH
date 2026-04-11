using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Manufacturing.Services;

namespace EVE.IPH.Domain.Manufacturing.Tests.Services;

public sealed class OwnedBlueprintWorkflowServiceTests
{
    [Fact]
    public async Task GetBlueprintsByOwnersAsync_UsesViewRepository()
    {
        FakeOwnedBlueprintRepository writeRepository = new();
        FakeOwnedBlueprintViewRepository viewRepository = new();
        viewRepository.Blueprints = [new OwnedBlueprintViewRecord(90000001, "Kara Maken", false, 7000001, 60015068, 28607, "Orca Blueprint", 1, 10, 20, -1, 1, true, true)];

        OwnedBlueprintWorkflowService sut = new(writeRepository, viewRepository);

        Result<IReadOnlyList<OwnedBlueprintViewRecord>> result = await sut.GetBlueprintsByOwnersAsync([90000001]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].OwnerName.Should().Be("Kara Maken");
    }

    [Fact]
    public async Task SaveBlueprintAsync_UsesWriteRepository()
    {
        FakeOwnedBlueprintRepository writeRepository = new();
        OwnedBlueprintWorkflowService sut = new(writeRepository, new FakeOwnedBlueprintViewRepository());
        OwnedBlueprintRecord blueprint = new(90000001, new ItemId(7000001), 60015068, new BlueprintId(28607), "Orca Blueprint", 1, 10, 20, -1, 1, true, true);

        Result<OwnedBlueprintRecord> result = await sut.SaveBlueprintAsync(blueprint);

        result.IsSuccess.Should().BeTrue();
        writeRepository.LastUpserted.Should().Be(blueprint);
    }

    private sealed class FakeOwnedBlueprintRepository : IOwnedBlueprintRepository
    {
        public OwnedBlueprintRecord? LastUpserted { get; private set; }

        public Task<Result<bool>> DeleteAsync(long userId, BlueprintId blueprintId, CancellationToken cancellationToken = default) => Task.FromResult(Result<bool>.Success(true));

        public Task<Result<bool>> DeleteByUserAsync(long userId, CancellationToken cancellationToken = default) => Task.FromResult(Result<bool>.Success(true));

        public Task<Result<IReadOnlyList<OwnedBlueprintRecord>>> GetByUserAsync(long userId, CancellationToken cancellationToken = default) => Task.FromResult(Result<IReadOnlyList<OwnedBlueprintRecord>>.Success([]));

        public Task<Result<IReadOnlyList<OwnedBlueprintRecord>>> GetByUsersAsync(IReadOnlyList<long> userIds, CancellationToken cancellationToken = default) => Task.FromResult(Result<IReadOnlyList<OwnedBlueprintRecord>>.Success([]));

        public Task<Result<IReadOnlyList<OwnedBlueprintRecord>>> ReplaceAsync(long userId, IReadOnlyList<OwnedBlueprintRecord> blueprints, CancellationToken cancellationToken = default) => Task.FromResult(Result<IReadOnlyList<OwnedBlueprintRecord>>.Success(blueprints));

        public Task<Result<OwnedBlueprintRecord>> UpsertAsync(OwnedBlueprintRecord record, CancellationToken cancellationToken = default)
        {
            LastUpserted = record;
            return Task.FromResult(Result<OwnedBlueprintRecord>.Success(record));
        }
    }

    private sealed class FakeOwnedBlueprintViewRepository : IOwnedBlueprintViewRepository
    {
        public IReadOnlyList<OwnedBlueprintViewRecord> Blueprints { get; set; } = [];

        public Task<Result<IReadOnlyList<OwnedBlueprintViewRecord>>> GetByOwnersAsync(IReadOnlyList<long> ownerIds, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<IReadOnlyList<OwnedBlueprintViewRecord>>.Success(Blueprints));
    }
}