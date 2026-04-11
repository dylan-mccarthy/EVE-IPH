namespace EVE.IPH.UI.Avalonia.Services;

public interface IIndustryJobsScreenService
{
    IndustryJobsScreenData GetScreenData(DateTimeOffset now);
}