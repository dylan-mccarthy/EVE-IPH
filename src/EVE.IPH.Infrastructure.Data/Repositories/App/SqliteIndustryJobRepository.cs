using System.Globalization;
using Dapper;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Connections;

namespace EVE.IPH.Infrastructure.Data.Repositories.App;

public sealed class SqliteIndustryJobRepository : IIndustryJobRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteIndustryJobRepository(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<IReadOnlyList<IndustryJobRecord>>> GetByInstallerIdAsync(
        CharacterId installerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = """
                SELECT jobID, installerID, facilityID, locationID, activityID, blueprintID, blueprintTypeID,
                       blueprintLocationID, outputLocationID, runs, cost, licensedRuns, probability,
                       productTypeID, status, duration, startDate, endDate, pauseDate, completedDate,
                       completedCharacterID, successfulRuns, JobType
                FROM INDUSTRY_JOBS
                WHERE installerID = @InstallerId
                ORDER BY endDate DESC, jobID DESC
                """;

            IEnumerable<IndustryJobDto> rows = await connection.QueryAsync<IndustryJobDto>(
                new CommandDefinition(sql, new { InstallerId = installerId.Value }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return Result<IReadOnlyList<IndustryJobRecord>>.Success(rows.Select(MapRecord).ToList());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<IndustryJobRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<IReadOnlyList<IndustryJobRecord>>> ReplaceAsync(
        CharacterId installerId,
        IndustryJobScope scope,
        IReadOnlyList<IndustryJobRecord> jobs,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jobs);

        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            connection.Open();
            using System.Data.IDbTransaction transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM INDUSTRY_JOBS WHERE installerID = @InstallerId AND JobType = @JobType",
                    new { InstallerId = installerId.Value, JobType = (int)scope },
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            foreach (IndustryJobRecord job in jobs)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO INDUSTRY_JOBS (
                            jobID, installerID, facilityID, locationID, activityID, blueprintID, blueprintTypeID,
                            blueprintLocationID, outputLocationID, runs, cost, licensedRuns, probability,
                            productTypeID, status, duration, startDate, endDate, pauseDate, completedDate,
                            completedCharacterID, successfulRuns, JobType)
                        VALUES (
                            @JobId, @InstallerId, @FacilityId, @LocationId, @ActivityId, @BlueprintId, @BlueprintTypeId,
                            @BlueprintLocationId, @OutputLocationId, @Runs, @Cost, @LicensedRuns, @Probability,
                            @ProductTypeId, @Status, @Duration, @StartDate, @EndDate, @PauseDate, @CompletedDate,
                            @CompletedCharacterId, @SuccessfulRuns, @Scope)
                        """,
                        ToParam(job),
                        transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            transaction.Commit();

            return await GetByInstallerIdAsync(installerId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<IndustryJobRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    private static object ToParam(IndustryJobRecord job) => new
    {
        JobId = job.JobId,
        InstallerId = job.InstallerId.Value,
        job.FacilityId,
        job.LocationId,
        job.ActivityId,
        job.BlueprintId,
        BlueprintTypeId = job.BlueprintTypeId.Value,
        job.BlueprintLocationId,
        job.OutputLocationId,
        job.Runs,
        job.Cost,
        job.LicensedRuns,
        job.Probability,
        ProductTypeId = job.ProductTypeId.HasValue ? (long?)job.ProductTypeId.Value.Value : null,
        job.Status,
        job.Duration,
        StartDate = FormatDate(job.StartDate),
        EndDate = FormatDate(job.EndDate),
        PauseDate = FormatDate(job.PauseDate),
        CompletedDate = FormatDate(job.CompletedDate),
        CompletedCharacterId = job.CompletedCharacterId.HasValue ? (long?)job.CompletedCharacterId.Value.Value : null,
        SuccessfulRuns = job.SuccessfulRuns,
        Scope = (int)job.Scope,
    };

    private static string? FormatDate(DateTimeOffset? value) =>
        value?.ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset? ParseDate(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private static IndustryJobRecord MapRecord(IndustryJobDto row) => new(
        row.jobID,
        new CharacterId(row.installerID),
        row.facilityID,
        row.locationID,
        row.activityID,
        row.blueprintID,
        new TypeId(row.blueprintTypeID),
        row.blueprintLocationID,
        row.outputLocationID,
        row.runs,
        row.cost,
        row.licensedRuns,
        row.probability,
        row.productTypeID.HasValue ? Maybe<TypeId>.Some(new TypeId(row.productTypeID.Value)) : Maybe<TypeId>.None,
        row.status,
        row.duration,
        ParseDate(row.startDate),
        ParseDate(row.endDate),
        ParseDate(row.pauseDate),
        ParseDate(row.completedDate),
        row.completedCharacterID.HasValue ? Maybe<CharacterId>.Some(new CharacterId(row.completedCharacterID.Value)) : Maybe<CharacterId>.None,
        row.successfulRuns,
        (IndustryJobScope)row.JobType);

    private sealed class IndustryJobDto
    {
        public long jobID { get; init; }
        public long installerID { get; init; }
        public long facilityID { get; init; }
        public long locationID { get; init; }
        public int activityID { get; init; }
        public long blueprintID { get; init; }
        public long blueprintTypeID { get; init; }
        public long blueprintLocationID { get; init; }
        public long outputLocationID { get; init; }
        public long runs { get; init; }
        public double cost { get; init; }
        public int licensedRuns { get; init; }
        public double probability { get; init; }
        public long? productTypeID { get; init; }
        public string status { get; init; } = string.Empty;
        public int duration { get; init; }
        public string? startDate { get; init; }
        public string? endDate { get; init; }
        public string? pauseDate { get; init; }
        public string? completedDate { get; init; }
        public long? completedCharacterID { get; init; }
        public int successfulRuns { get; init; }
        public int JobType { get; init; }
    }
}