using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Reads region, solar system, and station data from the EVE Static Data Export (SDE).
/// </summary>
public interface IRegionRepository
{
    Task<Maybe<RegionRecord>> GetRegionByIdAsync(RegionId regionId, CancellationToken cancellationToken = default);
    Task<Maybe<RegionRecord>> GetRegionByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RegionRecord>>> GetAllRegionsAsync(CancellationToken cancellationToken = default);
    Task<Maybe<SolarSystemRecord>> GetSolarSystemByIdAsync(SystemId systemId, CancellationToken cancellationToken = default);
    Task<Maybe<SolarSystemRecord>> GetSolarSystemByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Maybe<StationRecord>> GetStationByIdAsync(StationId stationId, CancellationToken cancellationToken = default);
}

/// <summary>A region from the SDE.</summary>
public sealed record RegionRecord(RegionId RegionId, string Name);

/// <summary>A solar system from the SDE.</summary>
public sealed record SolarSystemRecord(SystemId SystemId, string Name, RegionId RegionId, double SecurityLevel);

/// <summary>An NPC station from the SDE.</summary>
public sealed record StationRecord(StationId StationId, string Name, SystemId SystemId, RegionId RegionId);
