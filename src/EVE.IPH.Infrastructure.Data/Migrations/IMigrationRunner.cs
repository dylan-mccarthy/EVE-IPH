namespace EVE.IPH.Infrastructure.Data.Migrations;

/// <summary>Runs pending schema migrations against the application database.</summary>
public interface IMigrationRunner
{
    Task RunAsync(CancellationToken cancellationToken = default);
}
