using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace server.Infrastructure;

public interface ISqliteConnectionFactory
{
    SqliteConnection Create();
}

public sealed class SqliteOptions
{
    public string? DbPath { get; set; }
}

public sealed class SqliteConnectionFactory : ISqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(IOptions<SqliteOptions> options)
    {
        var path = options.Value.DbPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("Data:DbPath is not configured.");
        }

        // Use WAL and shared cache where appropriate.
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        };

        _connectionString = builder.ConnectionString;
    }

    public SqliteConnection Create()
    {
        var conn = new SqliteConnection(_connectionString);
        return conn;
    }
}
