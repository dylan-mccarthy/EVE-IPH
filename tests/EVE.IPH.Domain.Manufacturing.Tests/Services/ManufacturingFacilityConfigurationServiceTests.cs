using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Models;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Manufacturing.Services;

namespace EVE.IPH.Domain.Manufacturing.Tests.Services;

public sealed class ManufacturingFacilityConfigurationServiceTests
{
    [Fact]
    public async Task GetFacilityAsync_UpwellFacility_IncludesStructureAndInstalledModules()
    {
        FakeIndustryFacilityRepository repository = new();
        CharacterId characterId = new(90000001);
        IndustryFacilityConfigurationRecord configuration = new(
            characterId,
            FacilityProductionType.Manufacturing,
            102938,
            "Tatara Alpha",
            IndustryFacilityKind.UpwellStructure,
            10000002,
            "The Forge",
            30000142,
            "Jita",
            0.9,
            0.04,
            11,
            true,
            true,
            true,
            false,
            2,
            0.04,
            Maybe<double>.None,
            Maybe<double>.None,
            Maybe<double>.Some(0.95));
        repository.Facility = Maybe<IndustryFacilityConfigurationRecord>.Some(configuration);
        repository.Structure = Maybe<IndustryStructureRecord>.Some(new IndustryStructureRecord(102938, "Tatara Alpha", 35835, 30000142, 10000002, Maybe<long>.Some(98000001), true, DateTimeOffset.UtcNow));
        repository.Modules = [
            new IndustryFacilityModuleRecord(characterId, FacilityProductionType.Manufacturing, 102938, 6001),
            new IndustryFacilityModuleRecord(characterId, FacilityProductionType.Manufacturing, 102938, 6002),
        ];

        ManufacturingFacilityConfigurationService sut = new(repository);

        Result<Maybe<ResolvedIndustryFacilityConfiguration>> result = await sut.GetFacilityAsync(characterId, FacilityProductionType.Manufacturing);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasValue.Should().BeTrue();
        result.Value.Value.Configuration.Should().Be(configuration);
        result.Value.Value.Structure.HasValue.Should().BeTrue();
        result.Value.Value.InstalledModules.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFacilitiesAsync_WhenModuleLookupFails_ReturnsFailure()
    {
        FakeIndustryFacilityRepository repository = new();
        CharacterId characterId = new(90000001);
        repository.Facilities = [new IndustryFacilityConfigurationRecord(
            characterId,
            FacilityProductionType.Reactions,
            55,
            "Reaction Farm",
            IndustryFacilityKind.Station,
            10000002,
            "The Forge",
            30000142,
            "Jita",
            0.9,
            0.03,
            0,
            true,
            true,
            true,
            false,
            -1,
            0,
            Maybe<double>.None,
            Maybe<double>.None,
            Maybe<double>.None)];
        repository.ModuleFailure = Result<IReadOnlyList<IndustryFacilityModuleRecord>>.Failure("DB_ERROR", "module lookup failed");

        ManufacturingFacilityConfigurationService sut = new(repository);

        Result<IReadOnlyList<ResolvedIndustryFacilityConfiguration>> result = await sut.GetFacilitiesAsync(characterId);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("module lookup failed");
    }

    private sealed class FakeIndustryFacilityRepository : IIndustryFacilityRepository
    {
        public Maybe<IndustryFacilityConfigurationRecord> Facility { get; set; } = Maybe<IndustryFacilityConfigurationRecord>.None;

        public IReadOnlyList<IndustryFacilityConfigurationRecord> Facilities { get; set; } = [];

        public Maybe<IndustryStructureRecord> Structure { get; set; } = Maybe<IndustryStructureRecord>.None;

        public IReadOnlyList<IndustryFacilityModuleRecord> Modules { get; set; } = [];

        public Result<IReadOnlyList<IndustryFacilityModuleRecord>> ModuleFailure { get; set; } = Result<IReadOnlyList<IndustryFacilityModuleRecord>>.Success([]);

        public Task<Result<bool>> DeleteFacilityAsync(CharacterId characterId, FacilityProductionType productionType, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<bool>.Success(true));

        public Task<Result<bool>> DeleteStructureAsync(long structureId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<bool>.Success(true));

        public Task<Result<Maybe<IndustryFacilityConfigurationRecord>>> GetFacilityAsync(CharacterId characterId, FacilityProductionType productionType, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<Maybe<IndustryFacilityConfigurationRecord>>.Success(Facility));

        public Task<Result<IReadOnlyList<IndustryFacilityConfigurationRecord>>> GetFacilitiesAsync(CharacterId characterId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<IReadOnlyList<IndustryFacilityConfigurationRecord>>.Success(Facilities));

        public Task<Result<IReadOnlyList<IndustryFacilityModuleRecord>>> GetInstalledModulesAsync(CharacterId characterId, FacilityProductionType productionType, long facilityId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ModuleFailure.IsFailure ? ModuleFailure : Result<IReadOnlyList<IndustryFacilityModuleRecord>>.Success(Modules));

        public Task<Result<Maybe<IndustryStructureRecord>>> GetStructureAsync(long structureId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<Maybe<IndustryStructureRecord>>.Success(Structure));

        public Task<Result<IReadOnlyList<IndustryStructureRecord>>> GetStructuresAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<IReadOnlyList<IndustryStructureRecord>>.Success(Structure.HasValue ? [Structure.Value] : []));

        public Task<Result<IReadOnlyList<IndustryFacilityModuleRecord>>> ReplaceInstalledModulesAsync(CharacterId characterId, FacilityProductionType productionType, long facilityId, IReadOnlyList<int> moduleTypeIds, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<IReadOnlyList<IndustryFacilityModuleRecord>>.Success([]));

        public Task<Result<IndustryFacilityConfigurationRecord>> UpsertFacilityAsync(IndustryFacilityConfigurationRecord configuration, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<IndustryFacilityConfigurationRecord>.Success(configuration));

        public Task<Result<IndustryStructureRecord>> UpsertStructureAsync(IndustryStructureRecord structure, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<IndustryStructureRecord>.Success(structure));
    }
}