using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Models;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Repositories.App;

namespace EVE.IPH.Infrastructure.Data.Integration.Tests;

public sealed class SqliteIndustryFacilityRepositoryTests : IDisposable
{
    private readonly InMemoryDbFixture _fixture = new();
    private readonly IIndustryFacilityRepository _sut;

    public SqliteIndustryFacilityRepositoryTests()
    {
        _sut = new SqliteIndustryFacilityRepository(_fixture.ConnectionFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task UpsertFacilityAsync_ThenGetFacilityAsync_RoundTripsConfiguration()
    {
        IndustryFacilityConfigurationRecord record = new(
            new CharacterId(90000001),
            FacilityProductionType.Manufacturing,
            102938,
            "Tatara Alpha",
            IndustryFacilityKind.UpwellStructure,
            10000002,
            "The Forge",
            30000142,
            "Jita",
            0.9,
            0.0412,
            12.5,
            true,
            false,
            true,
            false,
            3,
            0.04,
            Maybe<double>.Some(0.98),
            Maybe<double>.Some(0.9),
            Maybe<double>.None);

        Result<IndustryFacilityConfigurationRecord> upsertResult = await _sut.UpsertFacilityAsync(record);
        Result<Maybe<IndustryFacilityConfigurationRecord>> getResult = await _sut.GetFacilityAsync(record.CharacterId, record.ProductionType);

        upsertResult.IsSuccess.Should().BeTrue();
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.HasValue.Should().BeTrue();
        getResult.Value.Value.Should().BeEquivalentTo(record);
    }

    [Fact]
    public async Task ReplaceInstalledModulesAsync_ReplacesDistinctModulesForFacility()
    {
        CharacterId characterId = new(90000001);

        Result<IReadOnlyList<IndustryFacilityModuleRecord>> result = await _sut.ReplaceInstalledModulesAsync(
            characterId,
            FacilityProductionType.Manufacturing,
            102938,
            [6001, 6002, 6002, 6003]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(module => module.ModuleTypeId).Should().Equal([6001, 6002, 6003]);
    }

    [Fact]
    public async Task UpsertStructureAsync_ThenGetStructuresAsync_ReturnsSavedStructure()
    {
        IndustryStructureRecord structure = new(
            102938,
            "Tatara Alpha",
            35835,
            30000142,
            10000002,
            Maybe<long>.Some(98000001),
            true,
            new DateTimeOffset(2026, 4, 12, 0, 0, 0, TimeSpan.Zero));

        Result<IndustryStructureRecord> upsertResult = await _sut.UpsertStructureAsync(structure);
        Result<IReadOnlyList<IndustryStructureRecord>> getResult = await _sut.GetStructuresAsync();

        upsertResult.IsSuccess.Should().BeTrue();
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Should().ContainSingle();
        getResult.Value[0].Should().BeEquivalentTo(structure);
    }
}