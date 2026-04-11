using Dapper;
using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Models;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Connections;

namespace EVE.IPH.Infrastructure.Data.Repositories.App;

public sealed class SqliteIndustryFacilityRepository : IIndustryFacilityRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteIndustryFacilityRepository(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<IReadOnlyList<IndustryStructureRecord>>> GetStructuresAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT STRUCTURE_ID, STRUCTURE_NAME, STRUCTURE_TYPE_ID, SOLAR_SYSTEM_ID, REGION_ID, OWNER_CORPORATION_ID, IS_MANUAL_ENTRY, UPDATED_AT_UTC FROM INDUSTRY_STRUCTURES ORDER BY STRUCTURE_NAME";

            IEnumerable<IndustryStructureDto> rows = await connection.QueryAsync<IndustryStructureDto>(
                new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);

            return Result<IReadOnlyList<IndustryStructureRecord>>.Success(rows.Select(MapStructure).ToArray());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<IndustryStructureRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<Maybe<IndustryStructureRecord>>> GetStructureAsync(long structureId, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT STRUCTURE_ID, STRUCTURE_NAME, STRUCTURE_TYPE_ID, SOLAR_SYSTEM_ID, REGION_ID, OWNER_CORPORATION_ID, IS_MANUAL_ENTRY, UPDATED_AT_UTC FROM INDUSTRY_STRUCTURES WHERE STRUCTURE_ID = @StructureId";

            IndustryStructureDto? row = await connection.QueryFirstOrDefaultAsync<IndustryStructureDto>(
                new CommandDefinition(sql, new { StructureId = structureId }, cancellationToken: cancellationToken)).ConfigureAwait(false);

            return Result<Maybe<IndustryStructureRecord>>.Success(row is null ? Maybe<IndustryStructureRecord>.None : Maybe<IndustryStructureRecord>.Some(MapStructure(row)));
        }
        catch (Exception ex)
        {
            return Result<Maybe<IndustryStructureRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<IndustryStructureRecord>> UpsertStructureAsync(IndustryStructureRecord structure, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = """
                INSERT INTO INDUSTRY_STRUCTURES
                    (STRUCTURE_ID, STRUCTURE_NAME, STRUCTURE_TYPE_ID, SOLAR_SYSTEM_ID, REGION_ID, OWNER_CORPORATION_ID, IS_MANUAL_ENTRY, UPDATED_AT_UTC)
                VALUES
                    (@StructureId, @StructureName, @StructureTypeId, @SolarSystemId, @RegionId, @OwnerCorporationId, @IsManualEntry, @UpdatedAtUtc)
                ON CONFLICT(STRUCTURE_ID) DO UPDATE SET
                    STRUCTURE_NAME = excluded.STRUCTURE_NAME,
                    STRUCTURE_TYPE_ID = excluded.STRUCTURE_TYPE_ID,
                    SOLAR_SYSTEM_ID = excluded.SOLAR_SYSTEM_ID,
                    REGION_ID = excluded.REGION_ID,
                    OWNER_CORPORATION_ID = excluded.OWNER_CORPORATION_ID,
                    IS_MANUAL_ENTRY = excluded.IS_MANUAL_ENTRY,
                    UPDATED_AT_UTC = excluded.UPDATED_AT_UTC
                """;

            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                structure.StructureId,
                structure.StructureName,
                structure.StructureTypeId,
                structure.SolarSystemId,
                structure.RegionId,
                OwnerCorporationId = structure.OwnerCorporationId.HasValue ? (long?)structure.OwnerCorporationId.Value : null,
                IsManualEntry = structure.IsManualEntry ? 1 : 0,
                UpdatedAtUtc = structure.UpdatedAtUtc.UtcDateTime.ToString("O"),
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);

            return Result<IndustryStructureRecord>.Success(structure);
        }
        catch (Exception ex)
        {
            return Result<IndustryStructureRecord>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<bool>> DeleteStructureAsync(long structureId, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            int affected = await connection.ExecuteAsync(
                new CommandDefinition("DELETE FROM INDUSTRY_STRUCTURES WHERE STRUCTURE_ID = @StructureId", new { StructureId = structureId }, cancellationToken: cancellationToken)).ConfigureAwait(false);

            return Result<bool>.Success(affected > 0);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<IReadOnlyList<IndustryFacilityConfigurationRecord>>> GetFacilitiesAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = """
                SELECT CHARACTER_ID, PRODUCTION_TYPE, FACILITY_ID, FACILITY_NAME, FACILITY_KIND,
                       REGION_ID, REGION_NAME, SOLAR_SYSTEM_ID, SOLAR_SYSTEM_NAME, SOLAR_SYSTEM_SECURITY,
                       COST_INDEX, ACTIVITY_COST_PER_SECOND, INCLUDE_ACTIVITY_COST, INCLUDE_ACTIVITY_TIME,
                       INCLUDE_ACTIVITY_USAGE, CONVERT_TO_ORE, FACTION_WARFARE_UPGRADE_LEVEL, TAX_RATE,
                       MATERIAL_MULTIPLIER_OVERRIDE, TIME_MULTIPLIER_OVERRIDE, COST_MULTIPLIER_OVERRIDE
                FROM INDUSTRY_FACILITY_CONFIGURATIONS
                WHERE CHARACTER_ID = @CharacterId
                ORDER BY PRODUCTION_TYPE
                """;

            IEnumerable<IndustryFacilityConfigurationDto> rows = await connection.QueryAsync<IndustryFacilityConfigurationDto>(
                new CommandDefinition(sql, new { CharacterId = characterId.Value }, cancellationToken: cancellationToken)).ConfigureAwait(false);

            return Result<IReadOnlyList<IndustryFacilityConfigurationRecord>>.Success(rows.Select(MapFacility).ToArray());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<IndustryFacilityConfigurationRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<Maybe<IndustryFacilityConfigurationRecord>>> GetFacilityAsync(
        CharacterId characterId,
        FacilityProductionType productionType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = """
                SELECT CHARACTER_ID, PRODUCTION_TYPE, FACILITY_ID, FACILITY_NAME, FACILITY_KIND,
                       REGION_ID, REGION_NAME, SOLAR_SYSTEM_ID, SOLAR_SYSTEM_NAME, SOLAR_SYSTEM_SECURITY,
                       COST_INDEX, ACTIVITY_COST_PER_SECOND, INCLUDE_ACTIVITY_COST, INCLUDE_ACTIVITY_TIME,
                       INCLUDE_ACTIVITY_USAGE, CONVERT_TO_ORE, FACTION_WARFARE_UPGRADE_LEVEL, TAX_RATE,
                       MATERIAL_MULTIPLIER_OVERRIDE, TIME_MULTIPLIER_OVERRIDE, COST_MULTIPLIER_OVERRIDE
                FROM INDUSTRY_FACILITY_CONFIGURATIONS
                WHERE CHARACTER_ID = @CharacterId AND PRODUCTION_TYPE = @ProductionType
                """;

            IndustryFacilityConfigurationDto? row = await connection.QueryFirstOrDefaultAsync<IndustryFacilityConfigurationDto>(
                new CommandDefinition(sql, new { CharacterId = characterId.Value, ProductionType = (int)productionType }, cancellationToken: cancellationToken)).ConfigureAwait(false);

            return Result<Maybe<IndustryFacilityConfigurationRecord>>.Success(row is null ? Maybe<IndustryFacilityConfigurationRecord>.None : Maybe<IndustryFacilityConfigurationRecord>.Some(MapFacility(row)));
        }
        catch (Exception ex)
        {
            return Result<Maybe<IndustryFacilityConfigurationRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<IndustryFacilityConfigurationRecord>> UpsertFacilityAsync(
        IndustryFacilityConfigurationRecord configuration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = """
                INSERT INTO INDUSTRY_FACILITY_CONFIGURATIONS
                    (CHARACTER_ID, PRODUCTION_TYPE, FACILITY_ID, FACILITY_NAME, FACILITY_KIND,
                     REGION_ID, REGION_NAME, SOLAR_SYSTEM_ID, SOLAR_SYSTEM_NAME, SOLAR_SYSTEM_SECURITY,
                     COST_INDEX, ACTIVITY_COST_PER_SECOND, INCLUDE_ACTIVITY_COST, INCLUDE_ACTIVITY_TIME,
                     INCLUDE_ACTIVITY_USAGE, CONVERT_TO_ORE, FACTION_WARFARE_UPGRADE_LEVEL, TAX_RATE,
                     MATERIAL_MULTIPLIER_OVERRIDE, TIME_MULTIPLIER_OVERRIDE, COST_MULTIPLIER_OVERRIDE)
                VALUES
                    (@CharacterId, @ProductionType, @FacilityId, @FacilityName, @FacilityKind,
                     @RegionId, @RegionName, @SolarSystemId, @SolarSystemName, @SolarSystemSecurity,
                     @CostIndex, @ActivityCostPerSecond, @IncludeActivityCost, @IncludeActivityTime,
                     @IncludeActivityUsage, @ConvertToOre, @FactionWarfareUpgradeLevel, @TaxRate,
                     @MaterialMultiplierOverride, @TimeMultiplierOverride, @CostMultiplierOverride)
                ON CONFLICT(CHARACTER_ID, PRODUCTION_TYPE) DO UPDATE SET
                    FACILITY_ID = excluded.FACILITY_ID,
                    FACILITY_NAME = excluded.FACILITY_NAME,
                    FACILITY_KIND = excluded.FACILITY_KIND,
                    REGION_ID = excluded.REGION_ID,
                    REGION_NAME = excluded.REGION_NAME,
                    SOLAR_SYSTEM_ID = excluded.SOLAR_SYSTEM_ID,
                    SOLAR_SYSTEM_NAME = excluded.SOLAR_SYSTEM_NAME,
                    SOLAR_SYSTEM_SECURITY = excluded.SOLAR_SYSTEM_SECURITY,
                    COST_INDEX = excluded.COST_INDEX,
                    ACTIVITY_COST_PER_SECOND = excluded.ACTIVITY_COST_PER_SECOND,
                    INCLUDE_ACTIVITY_COST = excluded.INCLUDE_ACTIVITY_COST,
                    INCLUDE_ACTIVITY_TIME = excluded.INCLUDE_ACTIVITY_TIME,
                    INCLUDE_ACTIVITY_USAGE = excluded.INCLUDE_ACTIVITY_USAGE,
                    CONVERT_TO_ORE = excluded.CONVERT_TO_ORE,
                    FACTION_WARFARE_UPGRADE_LEVEL = excluded.FACTION_WARFARE_UPGRADE_LEVEL,
                    TAX_RATE = excluded.TAX_RATE,
                    MATERIAL_MULTIPLIER_OVERRIDE = excluded.MATERIAL_MULTIPLIER_OVERRIDE,
                    TIME_MULTIPLIER_OVERRIDE = excluded.TIME_MULTIPLIER_OVERRIDE,
                    COST_MULTIPLIER_OVERRIDE = excluded.COST_MULTIPLIER_OVERRIDE
                """;

            await connection.ExecuteAsync(new CommandDefinition(sql, ToFacilityParams(configuration), cancellationToken: cancellationToken)).ConfigureAwait(false);
            return Result<IndustryFacilityConfigurationRecord>.Success(configuration);
        }
        catch (Exception ex)
        {
            return Result<IndustryFacilityConfigurationRecord>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<bool>> DeleteFacilityAsync(
        CharacterId characterId,
        FacilityProductionType productionType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            int affected = await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM INDUSTRY_FACILITY_CONFIGURATIONS WHERE CHARACTER_ID = @CharacterId AND PRODUCTION_TYPE = @ProductionType",
                new { CharacterId = characterId.Value, ProductionType = (int)productionType },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            return Result<bool>.Success(affected > 0);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<IReadOnlyList<IndustryFacilityModuleRecord>>> GetInstalledModulesAsync(
        CharacterId characterId,
        FacilityProductionType productionType,
        long facilityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT CHARACTER_ID, PRODUCTION_TYPE, FACILITY_ID, MODULE_TYPE_ID FROM INDUSTRY_FACILITY_MODULES WHERE CHARACTER_ID = @CharacterId AND PRODUCTION_TYPE = @ProductionType AND FACILITY_ID = @FacilityId ORDER BY MODULE_TYPE_ID";

            IEnumerable<IndustryFacilityModuleDto> rows = await connection.QueryAsync<IndustryFacilityModuleDto>(
                new CommandDefinition(sql, new { CharacterId = characterId.Value, ProductionType = (int)productionType, FacilityId = facilityId }, cancellationToken: cancellationToken)).ConfigureAwait(false);

            return Result<IReadOnlyList<IndustryFacilityModuleRecord>>.Success(rows.Select(MapModule).ToArray());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<IndustryFacilityModuleRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<IReadOnlyList<IndustryFacilityModuleRecord>>> ReplaceInstalledModulesAsync(
        CharacterId characterId,
        FacilityProductionType productionType,
        long facilityId,
        IReadOnlyList<int> moduleTypeIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(moduleTypeIds);

        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            connection.Open();
            using System.Data.IDbTransaction transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM INDUSTRY_FACILITY_MODULES WHERE CHARACTER_ID = @CharacterId AND PRODUCTION_TYPE = @ProductionType AND FACILITY_ID = @FacilityId",
                new { CharacterId = characterId.Value, ProductionType = (int)productionType, FacilityId = facilityId },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            const string insertSql = "INSERT INTO INDUSTRY_FACILITY_MODULES (CHARACTER_ID, PRODUCTION_TYPE, FACILITY_ID, MODULE_TYPE_ID) VALUES (@CharacterId, @ProductionType, @FacilityId, @ModuleTypeId)";

            foreach (int moduleTypeId in moduleTypeIds.Distinct().OrderBy(id => id))
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    insertSql,
                    new { CharacterId = characterId.Value, ProductionType = (int)productionType, FacilityId = facilityId, ModuleTypeId = moduleTypeId },
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            transaction.Commit();
            return await GetInstalledModulesAsync(characterId, productionType, facilityId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<IndustryFacilityModuleRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    private static object ToFacilityParams(IndustryFacilityConfigurationRecord configuration) => new
    {
        CharacterId = configuration.CharacterId.Value,
        ProductionType = (int)configuration.ProductionType,
        configuration.FacilityId,
        configuration.FacilityName,
        FacilityKind = (int)configuration.FacilityKind,
        configuration.RegionId,
        configuration.RegionName,
        configuration.SolarSystemId,
        configuration.SolarSystemName,
        configuration.SolarSystemSecurity,
        configuration.CostIndex,
        configuration.ActivityCostPerSecond,
        IncludeActivityCost = configuration.IncludeActivityCost ? 1 : 0,
        IncludeActivityTime = configuration.IncludeActivityTime ? 1 : 0,
        IncludeActivityUsage = configuration.IncludeActivityUsage ? 1 : 0,
        ConvertToOre = configuration.ConvertToOre ? 1 : 0,
        configuration.FactionWarfareUpgradeLevel,
        configuration.TaxRate,
        MaterialMultiplierOverride = configuration.MaterialMultiplierOverride.HasValue ? (double?)configuration.MaterialMultiplierOverride.Value : null,
        TimeMultiplierOverride = configuration.TimeMultiplierOverride.HasValue ? (double?)configuration.TimeMultiplierOverride.Value : null,
        CostMultiplierOverride = configuration.CostMultiplierOverride.HasValue ? (double?)configuration.CostMultiplierOverride.Value : null,
    };

    private static IndustryStructureRecord MapStructure(IndustryStructureDto row) => new(
        row.STRUCTURE_ID,
        row.STRUCTURE_NAME,
        row.STRUCTURE_TYPE_ID,
        row.SOLAR_SYSTEM_ID,
        row.REGION_ID,
        row.OWNER_CORPORATION_ID.HasValue ? Maybe<long>.Some(row.OWNER_CORPORATION_ID.Value) : Maybe<long>.None,
        row.IS_MANUAL_ENTRY == 1,
        DateTimeOffset.Parse(row.UPDATED_AT_UTC, null, System.Globalization.DateTimeStyles.RoundtripKind));

    private static IndustryFacilityConfigurationRecord MapFacility(IndustryFacilityConfigurationDto row) => new(
        new CharacterId(row.CHARACTER_ID),
        (FacilityProductionType)row.PRODUCTION_TYPE,
        row.FACILITY_ID,
        row.FACILITY_NAME,
        (IndustryFacilityKind)row.FACILITY_KIND,
        row.REGION_ID,
        row.REGION_NAME,
        row.SOLAR_SYSTEM_ID,
        row.SOLAR_SYSTEM_NAME,
        row.SOLAR_SYSTEM_SECURITY,
        row.COST_INDEX,
        row.ACTIVITY_COST_PER_SECOND,
        row.INCLUDE_ACTIVITY_COST == 1,
        row.INCLUDE_ACTIVITY_TIME == 1,
        row.INCLUDE_ACTIVITY_USAGE == 1,
        row.CONVERT_TO_ORE == 1,
        row.FACTION_WARFARE_UPGRADE_LEVEL,
        row.TAX_RATE,
        row.MATERIAL_MULTIPLIER_OVERRIDE.HasValue ? Maybe<double>.Some(row.MATERIAL_MULTIPLIER_OVERRIDE.Value) : Maybe<double>.None,
        row.TIME_MULTIPLIER_OVERRIDE.HasValue ? Maybe<double>.Some(row.TIME_MULTIPLIER_OVERRIDE.Value) : Maybe<double>.None,
        row.COST_MULTIPLIER_OVERRIDE.HasValue ? Maybe<double>.Some(row.COST_MULTIPLIER_OVERRIDE.Value) : Maybe<double>.None);

    private static IndustryFacilityModuleRecord MapModule(IndustryFacilityModuleDto row) => new(
        new CharacterId(row.CHARACTER_ID),
        (FacilityProductionType)row.PRODUCTION_TYPE,
        row.FACILITY_ID,
        row.MODULE_TYPE_ID);

    private sealed class IndustryStructureDto
    {
        public long STRUCTURE_ID { get; init; }
        public string STRUCTURE_NAME { get; init; } = string.Empty;
        public int STRUCTURE_TYPE_ID { get; init; }
        public long SOLAR_SYSTEM_ID { get; init; }
        public long REGION_ID { get; init; }
        public long? OWNER_CORPORATION_ID { get; init; }
        public int IS_MANUAL_ENTRY { get; init; }
        public string UPDATED_AT_UTC { get; init; } = string.Empty;
    }

    private sealed class IndustryFacilityConfigurationDto
    {
        public long CHARACTER_ID { get; init; }
        public int PRODUCTION_TYPE { get; init; }
        public long FACILITY_ID { get; init; }
        public string FACILITY_NAME { get; init; } = string.Empty;
        public int FACILITY_KIND { get; init; }
        public long REGION_ID { get; init; }
        public string REGION_NAME { get; init; } = string.Empty;
        public long SOLAR_SYSTEM_ID { get; init; }
        public string SOLAR_SYSTEM_NAME { get; init; } = string.Empty;
        public double SOLAR_SYSTEM_SECURITY { get; init; }
        public double COST_INDEX { get; init; }
        public double ACTIVITY_COST_PER_SECOND { get; init; }
        public int INCLUDE_ACTIVITY_COST { get; init; }
        public int INCLUDE_ACTIVITY_TIME { get; init; }
        public int INCLUDE_ACTIVITY_USAGE { get; init; }
        public int CONVERT_TO_ORE { get; init; }
        public int FACTION_WARFARE_UPGRADE_LEVEL { get; init; }
        public double TAX_RATE { get; init; }
        public double? MATERIAL_MULTIPLIER_OVERRIDE { get; init; }
        public double? TIME_MULTIPLIER_OVERRIDE { get; init; }
        public double? COST_MULTIPLIER_OVERRIDE { get; init; }
    }

    private sealed class IndustryFacilityModuleDto
    {
        public long CHARACTER_ID { get; init; }
        public int PRODUCTION_TYPE { get; init; }
        public long FACILITY_ID { get; init; }
        public int MODULE_TYPE_ID { get; init; }
    }
}