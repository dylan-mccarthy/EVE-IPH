using EVE.IPH.Domain.Industry.Models;

namespace EVE.IPH.Domain.Industry.Services;

public interface IIndustryJobPresentationService
{
    IndustryJobDisplayRow Present(IndustryJobViewItem job, DateTimeOffset now);
}