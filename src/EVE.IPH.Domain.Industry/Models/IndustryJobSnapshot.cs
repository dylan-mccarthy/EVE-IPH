using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.Domain.Industry.Models;

public sealed record IndustryJobSnapshot(
    CharacterId InstallerId,
    IReadOnlyList<IndustryJob> Jobs,
    IndustryJobSummary Summary);