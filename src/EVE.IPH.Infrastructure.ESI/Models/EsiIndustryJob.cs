using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Infrastructure.ESI.Models;

public sealed record EsiIndustryJob(
    long JobId,
    CharacterId InstallerId,
    long FacilityId,
    long LocationId,
    int ActivityId,
    long BlueprintId,
    TypeId BlueprintTypeId,
    long BlueprintLocationId,
    long OutputLocationId,
    long Runs,
    double Cost,
    int LicensedRuns,
    double Probability,
    Maybe<TypeId> ProductTypeId,
    string Status,
    int Duration,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    DateTimeOffset? PauseDate,
    DateTimeOffset? CompletedDate,
    Maybe<CharacterId> CompletedCharacterId,
    int SuccessfulRuns,
    IndustryJobScope Scope);