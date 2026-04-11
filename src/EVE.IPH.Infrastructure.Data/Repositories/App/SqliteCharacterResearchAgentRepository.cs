using System.Globalization;
using Dapper;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Connections;

namespace EVE.IPH.Infrastructure.Data.Repositories.App;

/// <summary>SQLite-backed implementation of <see cref="ICharacterResearchAgentRepository"/>.</summary>
public sealed class SqliteCharacterResearchAgentRepository : ICharacterResearchAgentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteCharacterResearchAgentRepository(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<IReadOnlyList<CharacterResearchAgentRecord>>> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            IEnumerable<CharacterResearchAgentDto> rows = await connection.QueryAsync<CharacterResearchAgentDto>(
                new CommandDefinition(
                    """
                    SELECT CHARACTER_ID, AGENT_ID, SKILL_TYPE_ID, RP_PER_DAY, RESEARCH_START_DATE, REMAINDER_POINTS
                    FROM CURRENT_RESEARCH_AGENTS
                    WHERE CHARACTER_ID = @CharacterId
                    ORDER BY AGENT_ID
                    """,
                    new { CharacterId = characterId.Value },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            return Result<IReadOnlyList<CharacterResearchAgentRecord>>.Success(rows.Select(MapRecord).ToList());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<CharacterResearchAgentRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<IReadOnlyList<CharacterResearchAgentRecord>>> ReplaceAsync(
        CharacterId characterId,
        IReadOnlyList<CharacterResearchAgentRecord> researchAgents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(researchAgents);

        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            connection.Open();
            using System.Data.IDbTransaction transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM CURRENT_RESEARCH_AGENTS WHERE CHARACTER_ID = @CharacterId",
                    new { CharacterId = characterId.Value },
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            foreach (CharacterResearchAgentRecord researchAgent in researchAgents)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO CURRENT_RESEARCH_AGENTS (
                            AGENT_ID,
                            SKILL_TYPE_ID,
                            RP_PER_DAY,
                            RESEARCH_START_DATE,
                            REMAINDER_POINTS,
                            CHARACTER_ID)
                        VALUES (
                            @AgentId,
                            @SkillTypeId,
                            @PointsPerDay,
                            @ResearchStartDate,
                            @RemainderPoints,
                            @CharacterId)
                        """,
                        new
                        {
                            researchAgent.AgentId,
                            SkillTypeId = researchAgent.SkillTypeId.Value,
                            researchAgent.PointsPerDay,
                            ResearchStartDate = researchAgent.ResearchStartDate.ToString("O", CultureInfo.InvariantCulture),
                            researchAgent.RemainderPoints,
                            CharacterId = researchAgent.CharacterId.Value,
                        },
                        transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            transaction.Commit();
            return Result<IReadOnlyList<CharacterResearchAgentRecord>>.Success(researchAgents.OrderBy(agent => agent.AgentId).ToList());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<CharacterResearchAgentRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    private static CharacterResearchAgentRecord MapRecord(CharacterResearchAgentDto row) => new(
        new CharacterId(row.CHARACTER_ID),
        row.AGENT_ID,
        new TypeId(row.SKILL_TYPE_ID),
        row.RP_PER_DAY,
        DateTimeOffset.Parse(row.RESEARCH_START_DATE, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        row.REMAINDER_POINTS);

    private sealed class CharacterResearchAgentDto
    {
        public long CHARACTER_ID { get; init; }
        public long AGENT_ID { get; init; }
        public long SKILL_TYPE_ID { get; init; }
        public double RP_PER_DAY { get; init; }
        public string RESEARCH_START_DATE { get; init; } = string.Empty;
        public double REMAINDER_POINTS { get; init; }
    }
}