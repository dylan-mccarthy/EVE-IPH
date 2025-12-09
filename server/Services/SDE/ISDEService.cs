namespace server.Services.SDE;

/// <summary>
/// Service for querying EVE Static Data Export (SDE) information
/// </summary>
public interface ISDEService
{
    /// <summary>
    /// Get type information by type ID
    /// </summary>
    Task<TypeInfo?> GetTypeInfoAsync(long typeId, CancellationToken ct = default);
    
    /// <summary>
    /// Get multiple type information by type IDs
    /// </summary>
    Task<Dictionary<long, TypeInfo>> GetTypeInfoBatchAsync(IEnumerable<long> typeIds, CancellationToken ct = default);
    
    /// <summary>
    /// Get location name by location ID
    /// </summary>
    Task<string> GetLocationNameAsync(long locationId, CancellationToken ct = default);
    
    /// <summary>
    /// Get activity name by activity ID
    /// </summary>
    Task<string> GetActivityNameAsync(int activityId, CancellationToken ct = default);
}

/// <summary>
/// Type information from INVENTORY_TYPES table
/// </summary>
public sealed record TypeInfo(
    long TypeId,
    string TypeName,
    string GroupName,
    string CategoryName
);
