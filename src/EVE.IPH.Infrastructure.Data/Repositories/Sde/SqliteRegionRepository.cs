using Dapper;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Connections;

namespace EVE.IPH.Infrastructure.Data.Repositories.Sde;

/// <summary>SQLite-backed implementation of <see cref="IRegionRepository"/> reading from the EVE SDE.</summary>
public sealed class SqliteRegionRepository : IRegionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteRegionRepository(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    public async Task<Maybe<RegionRecord>> GetRegionByIdAsync(RegionId regionId, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT regionID, regionName FROM REGIONS WHERE regionID = @RegionId";

            RegionDto? row = await connection.QueryFirstOrDefaultAsync<RegionDto>(
                new CommandDefinition(sql, new { RegionId = regionId.Value }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return row is null ? Maybe<RegionRecord>.None : Maybe<RegionRecord>.Some(new RegionRecord(new RegionId(row.regionID), row.regionName));
        }
        catch (Exception)
        {
            return Maybe<RegionRecord>.None;
        }
    }

    public async Task<Maybe<RegionRecord>> GetRegionByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT regionID, regionName FROM REGIONS WHERE regionName = @Name";

            RegionDto? row = await connection.QueryFirstOrDefaultAsync<RegionDto>(
                new CommandDefinition(sql, new { Name = name }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return row is null ? Maybe<RegionRecord>.None : Maybe<RegionRecord>.Some(new RegionRecord(new RegionId(row.regionID), row.regionName));
        }
        catch (Exception)
        {
            return Maybe<RegionRecord>.None;
        }
    }

    public async Task<Result<IReadOnlyList<RegionRecord>>> GetAllRegionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT regionID, regionName FROM REGIONS ORDER BY regionName";

            IEnumerable<RegionDto> rows = await connection.QueryAsync<RegionDto>(
                new CommandDefinition(sql, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            IReadOnlyList<RegionRecord> regions = rows
                .Select(r => new RegionRecord(new RegionId(r.regionID), r.regionName))
                .ToList();

            return Result<IReadOnlyList<RegionRecord>>.Success(regions);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<RegionRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Maybe<SolarSystemRecord>> GetSolarSystemByIdAsync(SystemId systemId, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT solarSystemID, solarSystemName, regionID, SECURITY FROM SOLAR_SYSTEMS WHERE solarSystemID = @SystemId";

            SolarSystemDto? row = await connection.QueryFirstOrDefaultAsync<SolarSystemDto>(
                new CommandDefinition(sql, new { SystemId = systemId.Value }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return row is null ? Maybe<SolarSystemRecord>.None : Maybe<SolarSystemRecord>.Some(MapSolarSystem(row));
        }
        catch (Exception)
        {
            return Maybe<SolarSystemRecord>.None;
        }
    }

    public async Task<Maybe<SolarSystemRecord>> GetSolarSystemByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT solarSystemID, solarSystemName, regionID, SECURITY FROM SOLAR_SYSTEMS WHERE solarSystemName = @Name";

            SolarSystemDto? row = await connection.QueryFirstOrDefaultAsync<SolarSystemDto>(
                new CommandDefinition(sql, new { Name = name }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return row is null ? Maybe<SolarSystemRecord>.None : Maybe<SolarSystemRecord>.Some(MapSolarSystem(row));
        }
        catch (Exception)
        {
            return Maybe<SolarSystemRecord>.None;
        }
    }

    public async Task<Maybe<StationRecord>> GetStationByIdAsync(StationId stationId, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT STATION_ID, STATION_NAME, solarSystemID, regionID FROM STATIONS WHERE STATION_ID = @StationId";

            StationDto? row = await connection.QueryFirstOrDefaultAsync<StationDto>(
                new CommandDefinition(sql, new { StationId = stationId.Value }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return row is null ? Maybe<StationRecord>.None : Maybe<StationRecord>.Some(
                new StationRecord(new StationId(row.STATION_ID), row.STATION_NAME, new SystemId(row.solarSystemID), new RegionId(row.regionID)));
        }
        catch (Exception)
        {
            return Maybe<StationRecord>.None;
        }
    }

    private static SolarSystemRecord MapSolarSystem(SolarSystemDto row) => new(
        new SystemId(row.solarSystemID),
        row.solarSystemName,
        new RegionId(row.regionID),
        row.SECURITY);

    private sealed class RegionDto
    {
        public int regionID { get; init; }
        public string regionName { get; init; } = string.Empty;
    }

    private sealed class SolarSystemDto
    {
        public int solarSystemID { get; init; }
        public string solarSystemName { get; init; } = string.Empty;
        public int regionID { get; init; }
        public double SECURITY { get; init; }
    }

    private sealed class StationDto
    {
        public long STATION_ID { get; init; }
        public string STATION_NAME { get; init; } = string.Empty;
        public int solarSystemID { get; init; }
        public int regionID { get; init; }
    }
}
