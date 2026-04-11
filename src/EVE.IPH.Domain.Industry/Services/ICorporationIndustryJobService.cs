using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Industry.Models;

namespace EVE.IPH.Domain.Industry.Services;

public interface ICorporationIndustryJobService
{
    Task<Result<CorporationIndustryJobSnapshot>> GetAsync(
        CorporationId corporationId,
        CancellationToken cancellationToken = default);

    Task<Result<CorporationIndustryJobSnapshot>> RefreshAsync(
        CorporationId corporationId,
        CancellationToken cancellationToken = default);
}