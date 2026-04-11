using Dapper;
using EVE.IPH.Infrastructure.Data.Repositories.App;

namespace EVE.IPH.Infrastructure.Data.Integration.Tests;

public sealed class SqliteAssetReadRepositoryTests : IDisposable
{
    private readonly InMemoryDbFixture _fixture = new();
    private readonly IAssetReadRepository _sut;

    public SqliteAssetReadRepositoryTests()
    {
        _sut = new SqliteAssetReadRepository(_fixture.ConnectionFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task GetHydratedAssetsAsync_ReturnsHydratedAssetsFromStorage()
    {
        using System.Data.IDbConnection connection = _fixture.ConnectionFactory.CreateConnection();

        await connection.ExecuteAsync("INSERT INTO ITEM_LOOKUP (typeID, typeName, groupName, categoryName) VALUES (@typeID, @typeName, @groupName, @categoryName)",
            new { typeID = 587L, typeName = "Rifter", groupName = "Frigate", categoryName = "Ship" });
        await connection.ExecuteAsync("INSERT INTO INVENTORY_FLAGS (FlagID, FlagText, container, sort_order) VALUES (@FlagID, @FlagText, @container, @sort_order)",
            new { FlagID = 4, FlagText = "Hangar", container = 0, sort_order = 10 });
        await connection.ExecuteAsync("INSERT INTO SOLAR_SYSTEMS (solarSystemID, solarSystemName, regionID, SECURITY) VALUES (@solarSystemID, @solarSystemName, @regionID, @SECURITY)",
            new { solarSystemID = 30000142L, solarSystemName = "Jita", regionID = 10000002L, SECURITY = 0.9 });
        await connection.ExecuteAsync("INSERT INTO STATIONS (STATION_ID, STATION_NAME, SOLAR_SYSTEM_ID, regionID) VALUES (@STATION_ID, @STATION_NAME, @SOLAR_SYSTEM_ID, @regionID)",
            new { STATION_ID = 60003760L, STATION_NAME = "Jita IV - Moon 4 - Caldari Navy Assembly Plant", SOLAR_SYSTEM_ID = 30000142L, regionID = 10000002L });
        await connection.ExecuteAsync("INSERT INTO ASSETS (ID, ItemID, LocationID, TypeID, Quantity, Flag, IsSingleton, IsBPCopy, ItemName) VALUES (@ID, @ItemID, @LocationID, @TypeID, @Quantity, @Flag, @IsSingleton, @IsBPCopy, @ItemName)",
            new { ID = 90000001L, ItemID = 90000011L, LocationID = 60003760L, TypeID = 587L, Quantity = 3L, Flag = 4, IsSingleton = 0, IsBPCopy = 0, ItemName = "" });

        var result = await _sut.GetHydratedAssetsAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].TypeName.Should().Be("Rifter");
        result.Value[0].GroupName.Should().Be("Frigate");
        result.Value[0].CategoryName.Should().Be("Ship");
        result.Value[0].LocationName.Should().Be("Jita IV - Moon 4 - Caldari Navy Assembly Plant");
        result.Value[0].FlagText.Should().Be("Hangar");
    }
}