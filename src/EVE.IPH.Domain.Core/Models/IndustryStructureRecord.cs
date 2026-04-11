using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Models;

public sealed record IndustryStructureRecord(
    long StructureId,
    string StructureName,
    int StructureTypeId,
    long SolarSystemId,
    long RegionId,
    Maybe<long> OwnerCorporationId,
    bool IsManualEntry,
    DateTimeOffset UpdatedAtUtc);