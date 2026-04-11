using Dapper;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Infrastructure.Data.Repositories.App;

namespace EVE.IPH.Infrastructure.Data.Integration.Tests;

public sealed class SqliteOwnedBlueprintReadRepositoryTests : IDisposable
{
    private readonly InMemoryDbFixture _fixture = new();
    private readonly IOwnedBlueprintViewRepository _sut;

    public SqliteOwnedBlueprintReadRepositoryTests()
    {
        _sut = new SqliteOwnedBlueprintReadRepository(_fixture.ConnectionFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task GetByOwnersAsync_ReturnsCharacterAndCorporationBlueprintsInOneShape()
    {
        using System.Data.IDbConnection connection = _fixture.ConnectionFactory.CreateConnection();
        await connection.ExecuteAsync("INSERT INTO ESI_CHARACTER_DATA (CHARACTER_ID, CHARACTER_NAME, CORPORATION_ID, ALLIANCE_ID, IS_DEFAULT) VALUES (90000001, 'Kara Maken', 98000001, NULL, 1)");
        await connection.ExecuteAsync("INSERT INTO ESI_CORPORATION_CONNECTIONS (CORPORATION_ID, CORPORATION_NAME, AUTHORIZED_CHARACTER_ID, HAS_ASSET_ACCESS, HAS_INDUSTRY_JOB_ACCESS, HAS_BLUEPRINT_ACCESS, HAS_DIRECTOR_ROLE, HAS_FACTORY_MANAGER_ROLE) VALUES (98000001, 'Acme Holdings', 90000001, 0, 0, 1, 1, 0)");
        await connection.ExecuteAsync(
            """
            INSERT INTO OWNED_BLUEPRINTS (USER_ID, ITEM_ID, LOCATION_ID, BLUEPRINT_ID, BLUEPRINT_NAME, QUANTITY, ME, TE, RUNS, BP_TYPE, OWNED, SCANNED)
            VALUES
                (90000001, 7000001, 60015068, 28607, 'Character Blueprint', 1, 10, 20, -1, 1, 1, 1),
                (98000001, 7000002, 60015068, 28608, 'Corporation Blueprint', 1, 8, 16, -1, 1, 1, 1)
            """);

        var result = await _sut.GetByOwnersAsync([90000001, 98000001]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(record => record.OwnerName == "Kara Maken" && !record.IsCorporationOwner);
        result.Value.Should().Contain(record => record.OwnerName == "Acme Holdings" && record.IsCorporationOwner);
    }
}