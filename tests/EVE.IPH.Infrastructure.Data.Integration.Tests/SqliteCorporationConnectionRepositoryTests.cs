using Dapper;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Infrastructure.Data.Repositories.App;

namespace EVE.IPH.Infrastructure.Data.Integration.Tests;

public sealed class SqliteCorporationConnectionRepositoryTests : IDisposable
{
    private readonly InMemoryDbFixture _fixture = new();
    private readonly ICorporationConnectionRepository _sut;

    public SqliteCorporationConnectionRepositoryTests()
    {
        _sut = new SqliteCorporationConnectionRepository(_fixture.ConnectionFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task UpsertAsync_PersistsRoleQualifiedCapabilities()
    {
        CorporationConnectionRecord record = new(
            new CorporationId(98000001),
            "Acme Holdings",
            new CharacterId(90000001),
            true,
            false,
            true,
            true,
            false);

        var result = await _sut.UpsertAsync(record);

        result.IsSuccess.Should().BeTrue();

        using System.Data.IDbConnection connection = _fixture.ConnectionFactory.CreateConnection();
        var stored = await connection.QuerySingleAsync<(int Asset, int Jobs, int Blueprints, int Director, int FactoryManager)>(
            """
            SELECT HAS_ASSET_ACCESS AS Asset,
                   HAS_INDUSTRY_JOB_ACCESS AS Jobs,
                   HAS_BLUEPRINT_ACCESS AS Blueprints,
                   HAS_DIRECTOR_ROLE AS Director,
                   HAS_FACTORY_MANAGER_ROLE AS FactoryManager
            FROM ESI_CORPORATION_CONNECTIONS
            WHERE CORPORATION_ID = @CorporationId
            """,
            new { CorporationId = 98000001L });

        stored.Asset.Should().Be(1);
        stored.Jobs.Should().Be(0);
        stored.Blueprints.Should().Be(1);
        stored.Director.Should().Be(1);
        stored.FactoryManager.Should().Be(0);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsPersistedRoleFlags()
    {
        using System.Data.IDbConnection connection = _fixture.ConnectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            """
            INSERT INTO ESI_CORPORATION_CONNECTIONS (
                CORPORATION_ID, CORPORATION_NAME, AUTHORIZED_CHARACTER_ID,
                HAS_ASSET_ACCESS, HAS_INDUSTRY_JOB_ACCESS, HAS_BLUEPRINT_ACCESS,
                HAS_DIRECTOR_ROLE, HAS_FACTORY_MANAGER_ROLE)
            VALUES (
                @CorporationId, @CorporationName, @AuthorizedCharacterId,
                @HasAssetAccess, @HasIndustryJobAccess, @HasBlueprintAccess,
                @HasDirectorRole, @HasFactoryManagerRole)
            """,
            new
            {
                CorporationId = 98000002L,
                CorporationName = "Beta Logistics",
                AuthorizedCharacterId = 90000002L,
                HasAssetAccess = 0,
                HasIndustryJobAccess = 1,
                HasBlueprintAccess = 0,
                HasDirectorRole = 0,
                HasFactoryManagerRole = 1,
            });

        var result = await _sut.GetByIdAsync(new CorporationId(98000002));

        result.HasValue.Should().BeTrue();
        result.Value.HasIndustryJobAccess.Should().BeTrue();
        result.Value.HasAssetAccess.Should().BeFalse();
        result.Value.HasFactoryManagerRole.Should().BeTrue();
        result.Value.HasDirectorRole.Should().BeFalse();
    }
}