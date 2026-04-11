using EVE.IPH.Domain.Industry.Models;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed record IndustryJobsScreenData(
    IndustryJobSummary Summary,
    IReadOnlyList<IndustryJobDisplayRow> Rows);