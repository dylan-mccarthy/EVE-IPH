using Dapper;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Repositories.App;

namespace EVE.IPH.Infrastructure.Data.Integration.Tests;

public sealed class SqliteAssetRepositoryTests : IDisposable
{
    private readonly InMemoryDbFixture _fixture = new();
    private readonly IAssetRepository _sut;

    public SqliteAssetRepositoryTests()
    {
        _sut = new SqliteAssetRepository(_fixture.ConnectionFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task ReplaceAsync_ReplacesAssetsForSingleOwnerOnly()
    {
        using System.Data.IDbConnection connection = _fixture.ConnectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "INSERT INTO ASSETS (ID, ItemID, LocationID, TypeID, Quantity, Flag, IsSingleton, IsBPCopy, ItemName) VALUES (@ID, @ItemID, @LocationID, @TypeID, @Quantity, @Flag, @IsSingleton, @IsBPCopy, @ItemName)",
            new[]
            {
                new { ID = 90000001L, ItemID = 1L, LocationID = 60003760L, TypeID = 34L, Quantity = 10L, Flag = 5, IsSingleton = 0, IsBPCopy = 0, ItemName = "Old" },
                new { ID = 90000002L, ItemID = 2L, LocationID = 60003760L, TypeID = 35L, Quantity = 20L, Flag = 5, IsSingleton = 0, IsBPCopy = 0, ItemName = "Other Owner" },
            });

        CharacterId ownerId = new(90000001);
        Result<IReadOnlyList<StoredAssetRecord>> result = await _sut.ReplaceAsync(ownerId.Value, [
            new StoredAssetRecord(ownerId.Value, 100L, 60003760L, new TypeId(36), 30L, 4, true, true, "Fresh Asset"),
        ]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].ItemId.Should().Be(100L);

        long untouchedRowCount = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM ASSETS WHERE ID = 90000002");
        untouchedRowCount.Should().Be(1);
    }

    [Fact]
    public async Task GetByOwnerIdAsync_ReturnsOnlyRequestedOwnerAssets()
    {
        using System.Data.IDbConnection connection = _fixture.ConnectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "INSERT INTO ASSETS (ID, ItemID, LocationID, TypeID, Quantity, Flag, IsSingleton, IsBPCopy, ItemName) VALUES (@ID, @ItemID, @LocationID, @TypeID, @Quantity, @Flag, @IsSingleton, @IsBPCopy, @ItemName)",
            new[]
            {
                new { ID = 90000003L, ItemID = 200L, LocationID = 60003760L, TypeID = 37L, Quantity = 40L, Flag = 4, IsSingleton = 1, IsBPCopy = -1, ItemName = "Wanted" },
                new { ID = 90000004L, ItemID = 201L, LocationID = 60003760L, TypeID = 38L, Quantity = 50L, Flag = 4, IsSingleton = 0, IsBPCopy = 0, ItemName = "Ignored" },
            });

        Result<IReadOnlyList<StoredAssetRecord>> result = await _sut.GetByOwnerIdAsync(90000003);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].ItemName.Should().Be("Wanted");
        result.Value[0].IsBlueprintCopy.Should().BeTrue();
    }
}