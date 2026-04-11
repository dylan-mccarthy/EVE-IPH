namespace EVE.IPH.UI.Avalonia.Services;

public interface IIndustryJobsCommandService
{
    Task<IndustryJobsScreenData> RefreshAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
}