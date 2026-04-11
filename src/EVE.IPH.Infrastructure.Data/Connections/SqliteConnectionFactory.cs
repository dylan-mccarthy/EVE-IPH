using System.Data;
using Microsoft.Data.Sqlite;

namespace EVE.IPH.Infrastructure.Data.Connections;

/// <summary>Creates SQLite connections from a fixed connection string.</summary>
public sealed class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection() => new SqliteConnection(_connectionString);
}
