using System.Reflection;
using System.Text.RegularExpressions;
using Dapper;
using EVE.IPH.Infrastructure.Data.Connections;

namespace EVE.IPH.Infrastructure.Data.Migrations;

/// <summary>
/// Applies embedded SQL migration scripts idempotently to the application database.
/// Scripts are embedded resources named <c>NNN_description.sql</c> under
/// <c>EVE.IPH.Infrastructure.Data.Migrations.Scripts</c>.
/// </summary>
public sealed class SqliteMigrationRunner : IMigrationRunner
{
    private static readonly Regex ScriptVersionRegex = new(@"^(\d+)_", RegexOptions.Compiled);

    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteMigrationRunner(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
        connection.Open();

        await EnsureMigrationsTableAsync(connection, cancellationToken).ConfigureAwait(false);

        IEnumerable<(int Version, string Sql)> scripts = LoadScripts();

        foreach ((int version, string sql) in scripts)
        {
            bool alreadyApplied = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    "SELECT COUNT(1) FROM SCHEMA_MIGRATIONS WHERE version = @Version",
                    new { Version = version },
                    cancellationToken: cancellationToken))
                .ConfigureAwait(false) > 0;

            if (alreadyApplied)
                continue;

            using System.Data.IDbTransaction transaction = connection.BeginTransaction();
            try
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(sql, transaction: transaction, cancellationToken: cancellationToken))
                    .ConfigureAwait(false);

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        "INSERT INTO SCHEMA_MIGRATIONS (version, applied_at) VALUES (@Version, @AppliedAt)",
                        new { Version = version, AppliedAt = DateTime.UtcNow.ToString("O") },
                        transaction: transaction,
                        cancellationToken: cancellationToken))
                    .ConfigureAwait(false);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    private static async Task EnsureMigrationsTableAsync(System.Data.IDbConnection connection, CancellationToken cancellationToken)
    {
        const string ddl = """
            CREATE TABLE IF NOT EXISTS SCHEMA_MIGRATIONS (
                version    INTEGER NOT NULL,
                applied_at TEXT    NOT NULL,
                PRIMARY KEY (version)
            )
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(ddl, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    private static IEnumerable<(int Version, string Sql)> LoadScripts()
    {
        Assembly assembly = typeof(SqliteMigrationRunner).Assembly;
        string prefix = "EVE.IPH.Infrastructure.Data.Migrations.Scripts.";

        return assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(prefix, StringComparison.Ordinal) && n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .Select(name =>
            {
                string fileName = name[prefix.Length..];
                Match match = ScriptVersionRegex.Match(fileName);
                int version = match.Success ? int.Parse(match.Groups[1].Value) : 0;
                string sql = ReadEmbeddedResource(assembly, name);
                return (version, sql);
            })
            .Where(t => t.version > 0)
            .OrderBy(t => t.version);
    }

    private static string ReadEmbeddedResource(Assembly assembly, string resourceName)
    {
        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");

        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
