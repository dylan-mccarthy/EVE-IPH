namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ManufacturingSkillRequirement(
    long TypeId,
    int RequiredLevel);