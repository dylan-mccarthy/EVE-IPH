using Dapper;
using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Connections;

namespace EVE.IPH.Infrastructure.Data.Repositories.Sde;

/// <summary>SQLite-backed implementation of <see cref="IBlueprintRepository"/> reading from the EVE SDE.</summary>
public sealed class SqliteBlueprintRepository : IBlueprintRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteBlueprintRepository(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    public async Task<Maybe<BlueprintRecord>> GetBlueprintAsync(BlueprintId blueprintId, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = """
                SELECT ABF.BLUEPRINT_ID, ABF.ITEM_ID, IT.typeName, ABF.TECH_LEVEL,
                       ABF.MAX_PRODUCTION_LIMIT, ABF.BASE_PRODUCTION_TIME
                FROM ALL_BLUEPRINTS_FACT AS ABF
                INNER JOIN INVENTORY_TYPES AS IT ON ABF.ITEM_ID = IT.typeID
                WHERE ABF.BLUEPRINT_ID = @BlueprintId
                """;

            BlueprintDto? row = await connection.QueryFirstOrDefaultAsync<BlueprintDto>(
                new CommandDefinition(sql, new { BlueprintId = blueprintId.Value }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return row is null ? Maybe<BlueprintRecord>.None : Maybe<BlueprintRecord>.Some(MapBlueprint(row));
        }
        catch (Exception)
        {
            return Maybe<BlueprintRecord>.None;
        }
    }

    public async Task<Result<IReadOnlyList<BlueprintMaterial>>> GetMaterialsAsync(
        BlueprintId blueprintId,
        ActivityType activity,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = """
                SELECT ABM.MATERIAL_ID AS typeID, IT.typeName, ABM.QUANTITY
                FROM ALL_BLUEPRINT_MATERIALS_FACT AS ABM
                INNER JOIN INVENTORY_TYPES AS IT ON ABM.MATERIAL_ID = IT.typeID
                WHERE ABM.BLUEPRINT_ID = @BlueprintId AND ABM.ACTIVITY = @Activity
                """;

            IEnumerable<MaterialDto> rows = await connection.QueryAsync<MaterialDto>(
                new CommandDefinition(sql, new { BlueprintId = blueprintId.Value, Activity = (int)activity }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            IReadOnlyList<BlueprintMaterial> materials = rows
                .Select(r => new BlueprintMaterial(new TypeId(r.typeID), r.typeName, r.QUANTITY))
                .ToList();

            return Result<IReadOnlyList<BlueprintMaterial>>.Success(materials);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<BlueprintMaterial>>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<IReadOnlyList<SkillRequirement>>> GetRequiredSkillsAsync(
        BlueprintId blueprintId,
        ActivityType activity,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = """
                SELECT typeID, level
                FROM INDUSTRY_ACTIVITY_SKILLS
                WHERE blueprintTypeID = @BlueprintId AND activityID = @Activity
                """;

            IEnumerable<SkillDto> rows = await connection.QueryAsync<SkillDto>(
                new CommandDefinition(sql, new { BlueprintId = blueprintId.Value, Activity = (int)activity }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            IReadOnlyList<SkillRequirement> skills = rows
                .Select(r => new SkillRequirement(new TypeId(r.typeID), r.level))
                .ToList();

            return Result<IReadOnlyList<SkillRequirement>>.Success(skills);
        }
        catch (Exception)
        {
            // Table may not exist in all SDE versions; return empty list.
            return Result<IReadOnlyList<SkillRequirement>>.Success(Array.Empty<SkillRequirement>());
        }
    }

    private static BlueprintRecord MapBlueprint(BlueprintDto row) => new(
        new BlueprintId(row.BLUEPRINT_ID),
        new TypeId(row.ITEM_ID),
        row.typeName,
        (TechLevel)row.TECH_LEVEL,
        row.MAX_PRODUCTION_LIMIT,
        row.BASE_PRODUCTION_TIME,
        ResearchMeTime: 0,
        ResearchTeTime: 0,
        CopyTime: 0,
        InventionTime: 0);

    private sealed class BlueprintDto
    {
        public long BLUEPRINT_ID { get; init; }
        public long ITEM_ID { get; init; }
        public string typeName { get; init; } = string.Empty;
        public int TECH_LEVEL { get; init; }
        public int MAX_PRODUCTION_LIMIT { get; init; }
        public long BASE_PRODUCTION_TIME { get; init; }
    }

    private sealed class MaterialDto
    {
        public long typeID { get; init; }
        public string typeName { get; init; } = string.Empty;
        public long QUANTITY { get; init; }
    }

    private sealed class SkillDto
    {
        public long typeID { get; init; }
        public int level { get; init; }
    }
}
