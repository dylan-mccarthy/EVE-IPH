using EVE.IPH.Domain.Characters.Models;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed record ResearchAgentsScreenData(
    ResearchAgentDatacoreSummary Summary,
    string StatusText);