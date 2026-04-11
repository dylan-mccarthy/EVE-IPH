using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.Domain.Industry.Models;

public sealed record CorporationIndustryJobSnapshot(
    CorporationId CorporationId,
    IReadOnlyList<IndustryJob> Jobs,
    IndustryJobSummary Summary);