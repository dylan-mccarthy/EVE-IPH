using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Industry.Models;

namespace EVE.IPH.Domain.Industry.Services;

public interface ICharacterIndustryJobService
{
    Task<Result<IndustryJobSnapshot>> GetAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    Task<Result<IndustryJobSnapshot>> RefreshAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);
}