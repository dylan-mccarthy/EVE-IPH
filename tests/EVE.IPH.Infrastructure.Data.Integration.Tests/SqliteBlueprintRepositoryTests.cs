using Dapper;
using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Repositories.Sde;

namespace EVE.IPH.Infrastructure.Data.Integration.Tests;

public sealed class SqliteBlueprintRepositoryTests : IDisposable
{
    private readonly InMemoryDbFixture _fixture = new();
    private readonly IBlueprintRepository _sut;

    public SqliteBlueprintRepositoryTests()
    {
        _sut = new SqliteBlueprintRepository(_fixture.ConnectionFactory);
        SeedData();
    }

    public void Dispose() => _fixture.Dispose();

    private void SeedData()
    {
        using System.Data.IDbConnection connection = _fixture.ConnectionFactory.CreateConnection();

        connection.Execute("INSERT OR IGNORE INTO INVENTORY_TYPES (typeID, typeName, groupID, volume, portionSize) VALUES (587, 'Rifter', 25, 2500.0, 1)");
        connection.Execute("INSERT OR IGNORE INTO INVENTORY_TYPES (typeID, typeName, groupID, volume, portionSize) VALUES (34, 'Tritanium', 18, 0.01, 1)");
        connection.Execute("INSERT OR IGNORE INTO INVENTORY_TYPES (typeID, typeName, groupID, volume, portionSize) VALUES (35, 'Pyerite', 18, 0.01, 1)");

        connection.Execute("INSERT OR IGNORE INTO ALL_BLUEPRINTS_FACT (BLUEPRINT_ID, ITEM_ID, TECH_LEVEL, MAX_PRODUCTION_LIMIT, BASE_PRODUCTION_TIME) VALUES (677, 587, 1, 300, 7200)");

        connection.Execute("INSERT OR IGNORE INTO ALL_BLUEPRINT_MATERIALS_FACT (BLUEPRINT_ID, MATERIAL_ID, QUANTITY, ACTIVITY) VALUES (677, 34, 100, 1)");
        connection.Execute("INSERT OR IGNORE INTO ALL_BLUEPRINT_MATERIALS_FACT (BLUEPRINT_ID, MATERIAL_ID, QUANTITY, ACTIVITY) VALUES (677, 35, 50, 1)");
    }

    [Fact]
    public async Task GetBlueprintAsync_ExistingBlueprint_ReturnsBlueprintRecord()
    {
        Maybe<BlueprintRecord> result = await _sut.GetBlueprintAsync(new BlueprintId(677));

        result.HasValue.Should().BeTrue();
        result.Value.BlueprintId.Value.Should().Be(677);
        result.Value.ProductTypeId.Value.Should().Be(587);
        result.Value.ProductName.Should().Be("Rifter");
        result.Value.TechLevel.Should().Be(TechLevel.T1);
        result.Value.MaxProductionLimit.Should().Be(300);
        result.Value.ManufacturingTime.Should().Be(7200);
    }

    [Fact]
    public async Task GetBlueprintAsync_MissingBlueprint_ReturnsNone()
    {
        Maybe<BlueprintRecord> result = await _sut.GetBlueprintAsync(new BlueprintId(999_999));

        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public async Task GetMaterialsAsync_ManufacturingActivity_ReturnsMaterials()
    {
        Result<IReadOnlyList<BlueprintMaterial>> result = await _sut.GetMaterialsAsync(new BlueprintId(677), ActivityType.Manufacturing);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(m => m.TypeId.Value == 34 && m.Quantity == 100);
        result.Value.Should().Contain(m => m.TypeId.Value == 35 && m.Quantity == 50);
    }

    [Fact]
    public async Task GetMaterialsAsync_MissingBlueprint_ReturnsEmptyList()
    {
        Result<IReadOnlyList<BlueprintMaterial>> result = await _sut.GetMaterialsAsync(new BlueprintId(999_999), ActivityType.Manufacturing);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRequiredSkillsAsync_WhenTableAbsent_ReturnsEmptyList()
    {
        // INDUSTRY_ACTIVITY_SKILLS table is not seeded; repository should return empty list gracefully.
        Result<IReadOnlyList<SkillRequirement>> result = await _sut.GetRequiredSkillsAsync(new BlueprintId(677), ActivityType.Manufacturing);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
