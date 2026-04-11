using EVE.IPH.Domain.Characters.Models;

namespace EVE.IPH.UI.Avalonia.Services;

public interface IResearchAgentsScreenService
{
    Task<ResearchAgentsScreenData> GetScreenDataAsync(CancellationToken cancellationToken = default);
}