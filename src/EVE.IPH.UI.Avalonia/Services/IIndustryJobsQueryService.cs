namespace EVE.IPH.UI.Avalonia.Services;

public interface IIndustryJobsQueryService
{
    Task<IndustryJobsScreenData> GetScreenDataAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
}

public sealed record IndustryJobsScreenData(
    EVE.IPH.Domain.Industry.Models.IndustryJobSummary Summary,
    IReadOnlyList<EVE.IPH.Domain.Industry.Models.IndustryJobDisplayRow> Rows,
    string StatusText);