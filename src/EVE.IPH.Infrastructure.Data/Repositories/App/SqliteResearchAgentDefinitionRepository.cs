using Dapper;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Connections;

namespace EVE.IPH.Infrastructure.Data.Repositories.App;

/// <summary>SQLite-backed implementation of <see cref="IResearchAgentDefinitionRepository"/>.</summary>
public sealed class SqliteResearchAgentDefinitionRepository : IResearchAgentDefinitionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteResearchAgentDefinitionRepository(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<IReadOnlyDictionary<long, ResearchAgentDefinitionRecord>>> GetByAgentIdsAsync(
        IReadOnlyCollection<long> agentIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agentIds);

        if (agentIds.Count == 0)
        {
            return Result<IReadOnlyDictionary<long, ResearchAgentDefinitionRecord>>.Success(new Dictionary<long, ResearchAgentDefinitionRecord>());
        }

        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            IEnumerable<ResearchAgentDefinitionDto> rows = await connection.QueryAsync<ResearchAgentDefinitionDto>(
                new CommandDefinition(
                    """
                    SELECT AGENT_ID, AGENT_NAME, RP_PER_DAY, LEVEL, STATION
                    FROM RESEARCH_AGENTS
                    WHERE AGENT_ID IN @AgentIds
                    """,
                    new { AgentIds = agentIds.ToArray() },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            IReadOnlyDictionary<long, ResearchAgentDefinitionRecord> mapped = rows
                .Select(MapRecord)
                .ToDictionary(record => record.AgentId);

            return Result<IReadOnlyDictionary<long, ResearchAgentDefinitionRecord>>.Success(mapped);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyDictionary<long, ResearchAgentDefinitionRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    private static ResearchAgentDefinitionRecord MapRecord(ResearchAgentDefinitionDto row) => new(
        row.AGENT_ID,
        row.AGENT_NAME,
        row.RP_PER_DAY,
        row.LEVEL,
        row.STATION);

    private sealed class ResearchAgentDefinitionDto
    {
        public long AGENT_ID { get; init; }
        public string AGENT_NAME { get; init; } = string.Empty;
        public double RP_PER_DAY { get; init; }
        public int LEVEL { get; init; }
        public string STATION { get; init; } = string.Empty;
    }
}