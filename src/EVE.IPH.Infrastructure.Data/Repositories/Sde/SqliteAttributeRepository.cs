using Dapper;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Infrastructure.Data.Connections;

namespace EVE.IPH.Infrastructure.Data.Repositories.Sde;

/// <summary>
/// Reads type attribute values from the SDE TYPE_ATTRIBUTES table.
/// Used internally to look up attributes such as tech level.
/// </summary>
public sealed class SqliteAttributeRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteAttributeRepository(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Returns the numeric value of <paramref name="attributeId"/> for <paramref name="typeId"/>,
    /// or <see langword="null"/> if the attribute is not present.
    /// </summary>
    public async Task<double?> GetTypeAttributeAsync(TypeId typeId, int attributeId, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = """
                SELECT valueFloat
                FROM TYPE_ATTRIBUTES
                WHERE typeID = @TypeId AND attributeID = @AttributeId
                """;

            return await connection.QueryFirstOrDefaultAsync<double?>(
                new CommandDefinition(sql, new { TypeId = typeId.Value, AttributeId = attributeId }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
