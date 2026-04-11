using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Industry.Models;

namespace EVE.IPH.Domain.Industry.Services;

public sealed class CorporationIndustryJobService(
    ICharacterRepository characterRepository,
    IIndustryJobRepository industryJobRepository,
    IIndustryJobDataSource industryJobDataSource,
    IIndustryJobService industryJobService,
    TimeProvider timeProvider) : ICorporationIndustryJobService
{
    private readonly ICharacterRepository _characterRepository = characterRepository ?? throw new ArgumentNullException(nameof(characterRepository));
    private readonly IIndustryJobRepository _industryJobRepository = industryJobRepository ?? throw new ArgumentNullException(nameof(industryJobRepository));
    private readonly IIndustryJobDataSource _industryJobDataSource = industryJobDataSource ?? throw new ArgumentNullException(nameof(industryJobDataSource));
    private readonly IIndustryJobService _industryJobService = industryJobService ?? throw new ArgumentNullException(nameof(industryJobService));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<Result<CorporationIndustryJobSnapshot>> GetAsync(
        CorporationId corporationId,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<CharacterRecord>> characters = await _characterRepository
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        if (characters.IsFailure)
        {
            return Result<CorporationIndustryJobSnapshot>.Failure(characters.Error);
        }

        CharacterId[] installerIds = GetCorporationInstallerIds(characters.Value, corporationId);
        List<IndustryJobRecord> records = new();

        foreach (CharacterId installerId in installerIds)
        {
            Result<IReadOnlyList<IndustryJobRecord>> storedJobs = await _industryJobRepository
                .GetByInstallerIdAsync(installerId, cancellationToken)
                .ConfigureAwait(false);

            if (storedJobs.IsFailure)
            {
                return Result<CorporationIndustryJobSnapshot>.Failure(storedJobs.Error);
            }

            records.AddRange(storedJobs.Value.Where(job => job.Scope == IndustryJobScope.Corporation));
        }

        return CreateSnapshot(corporationId, records);
    }

    public async Task<Result<CorporationIndustryJobSnapshot>> RefreshAsync(
        CorporationId corporationId,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<CharacterRecord>> characters = await _characterRepository
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        if (characters.IsFailure)
        {
            return Result<CorporationIndustryJobSnapshot>.Failure(characters.Error);
        }

        CharacterId[] installerIds = GetCorporationInstallerIds(characters.Value, corporationId);
        if (installerIds.Length == 0)
        {
            return Result<CorporationIndustryJobSnapshot>.Success(new CorporationIndustryJobSnapshot(corporationId, [], new IndustryJobSummary(0, 0, 0, 0, 0, 0)));
        }

        HashSet<long> installerIdSet = installerIds.Select(id => id.Value).ToHashSet();

        Result<IReadOnlyList<IndustryJobData>> currentJobs = await _industryJobDataSource
            .GetCorporationJobsAsync(corporationId, installerIds[0], cancellationToken)
            .ConfigureAwait(false);

        if (currentJobs.IsFailure)
        {
            return Result<CorporationIndustryJobSnapshot>.Failure(currentJobs.Error);
        }

        IReadOnlyList<IndustryJobRecord> normalizedJobs = currentJobs.Value
            .Where(job => installerIdSet.Contains(job.InstallerId.Value))
            .Select(job => new IndustryJobRecord(
                job.JobId,
                job.InstallerId,
                job.FacilityId,
                job.LocationId,
                NormalizeActivityId(job.ActivityId),
                job.BlueprintId,
                job.BlueprintTypeId,
                job.BlueprintLocationId,
                job.OutputLocationId,
                job.Runs,
                job.Cost,
                job.LicensedRuns,
                job.Probability,
                job.ProductTypeId,
                job.Status,
                job.Duration,
                job.StartDate,
                job.EndDate,
                job.PauseDate,
                job.CompletedDate,
                job.CompletedCharacterId,
                job.SuccessfulRuns,
                IndustryJobScope.Corporation))
            .ToArray();

        Dictionary<CharacterId, IReadOnlyList<IndustryJobRecord>> jobsByInstaller = installerIds.ToDictionary(
            installerId => installerId,
            installerId => (IReadOnlyList<IndustryJobRecord>)normalizedJobs.Where(job => job.InstallerId == installerId).ToArray());

        List<IndustryJobRecord> storedJobs = new();

        foreach ((CharacterId installerId, IReadOnlyList<IndustryJobRecord> installerJobs) in jobsByInstaller)
        {
            Result<IReadOnlyList<IndustryJobRecord>> replaceResult = await _industryJobRepository
                .ReplaceAsync(installerId, IndustryJobScope.Corporation, installerJobs, cancellationToken)
                .ConfigureAwait(false);

            if (replaceResult.IsFailure)
            {
                return Result<CorporationIndustryJobSnapshot>.Failure(replaceResult.Error);
            }

            storedJobs.AddRange(replaceResult.Value.Where(job => job.Scope == IndustryJobScope.Corporation));
        }

        return CreateSnapshot(corporationId, storedJobs);
    }

    private Result<CorporationIndustryJobSnapshot> CreateSnapshot(
        CorporationId corporationId,
        IReadOnlyList<IndustryJobRecord> records)
    {
        IReadOnlyList<IndustryJob> jobs = records
            .Select(record => new IndustryJob(
                record.JobId,
                record.InstallerId.Value,
                record.ActivityId,
                record.Status,
                record.StartDate,
                record.EndDate))
            .ToArray();

        IndustryJobSummary summary = _industryJobService.SummarizeCurrentJobs(jobs, _timeProvider.GetUtcNow());

        return Result<CorporationIndustryJobSnapshot>.Success(new CorporationIndustryJobSnapshot(corporationId, jobs, summary));
    }

    private static CharacterId[] GetCorporationInstallerIds(
        IReadOnlyList<CharacterRecord> characters,
        CorporationId corporationId)
    {
        return characters
            .Where(character => character.CorporationId == corporationId)
            .Select(character => character.CharacterId)
            .Distinct()
            .ToArray();
    }

    private static int NormalizeActivityId(int activityId) => activityId == 9 ? 11 : activityId;
}