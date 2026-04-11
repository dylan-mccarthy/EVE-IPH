using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.Domain.Assets.Services;
using EVE.IPH.Domain.Characters.Services;
using EVE.IPH.Domain.Core;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Industry.Models;
using EVE.IPH.Domain.Industry.Services;
using EVE.IPH.Domain.Manufacturing.Services;
using EVE.IPH.Infrastructure.ESI;
using EVE.IPH.Infrastructure.ESI.Interfaces;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class CharacterManagementService : ICharacterManagementCommandService
{
    private static readonly string[] RequiredScopes =
    [
        "esi-skills.read_skills.v1",
        "esi-characters.read_standings.v1",
        "esi-characters.read_agents_research.v1",
        "esi-industry.read_character_jobs.v1",
    ];

    private readonly ICharacterRepository _characterRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly ICharacterAssetService _characterAssetService;
    private readonly ICorporationAssetService _corporationAssetService;
    private readonly ICorporationBlueprintService _corporationBlueprintService;
    private readonly ICorporationCapabilityResolver _corporationCapabilityResolver;
    private readonly ICorporationConnectionRepository _corporationConnectionRepository;
    private readonly ICharacterService _characterService;
    private readonly ICharacterIndustryJobService _characterIndustryJobService;
    private readonly ICorporationIndustryJobService _corporationIndustryJobService;
    private readonly IIndustryJobRepository _industryJobRepository;
    private readonly IOwnedBlueprintRepository _ownedBlueprintRepository;
    private readonly IEsiClient _esiClient;
    private readonly IEsiInteractiveLoginService _interactiveLoginService;
    private readonly IResearchAgentService _researchAgentService;
    private readonly IEsiTokenStore _tokenStore;

    public CharacterManagementService(
        ICharacterRepository characterRepository,
        IAssetRepository assetRepository,
        ICharacterAssetService characterAssetService,
        ICorporationAssetService corporationAssetService,
        ICorporationBlueprintService corporationBlueprintService,
        ICorporationCapabilityResolver corporationCapabilityResolver,
        ICorporationConnectionRepository corporationConnectionRepository,
        ICharacterService characterService,
        ICharacterIndustryJobService characterIndustryJobService,
        ICorporationIndustryJobService corporationIndustryJobService,
        IIndustryJobRepository industryJobRepository,
        IOwnedBlueprintRepository ownedBlueprintRepository,
        IEsiClient esiClient,
        IEsiInteractiveLoginService interactiveLoginService,
        IResearchAgentService researchAgentService,
        IEsiTokenStore tokenStore)
    {
        _characterRepository = characterRepository ?? throw new ArgumentNullException(nameof(characterRepository));
        _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
        _characterAssetService = characterAssetService ?? throw new ArgumentNullException(nameof(characterAssetService));
        _corporationAssetService = corporationAssetService ?? throw new ArgumentNullException(nameof(corporationAssetService));
        _corporationBlueprintService = corporationBlueprintService ?? throw new ArgumentNullException(nameof(corporationBlueprintService));
        _corporationCapabilityResolver = corporationCapabilityResolver ?? throw new ArgumentNullException(nameof(corporationCapabilityResolver));
        _corporationConnectionRepository = corporationConnectionRepository ?? throw new ArgumentNullException(nameof(corporationConnectionRepository));
        _characterService = characterService ?? throw new ArgumentNullException(nameof(characterService));
        _characterIndustryJobService = characterIndustryJobService ?? throw new ArgumentNullException(nameof(characterIndustryJobService));
        _corporationIndustryJobService = corporationIndustryJobService ?? throw new ArgumentNullException(nameof(corporationIndustryJobService));
        _industryJobRepository = industryJobRepository ?? throw new ArgumentNullException(nameof(industryJobRepository));
        _ownedBlueprintRepository = ownedBlueprintRepository ?? throw new ArgumentNullException(nameof(ownedBlueprintRepository));
        _esiClient = esiClient ?? throw new ArgumentNullException(nameof(esiClient));
        _interactiveLoginService = interactiveLoginService ?? throw new ArgumentNullException(nameof(interactiveLoginService));
        _researchAgentService = researchAgentService ?? throw new ArgumentNullException(nameof(researchAgentService));
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
    }

    public async Task<Result<CharacterRecord>> AuthenticateAndRefreshAsync(CancellationToken cancellationToken = default)
    {
        Result<EVE.IPH.Infrastructure.ESI.EsiAccessToken> tokenResult = await _interactiveLoginService
            .AuthenticateAsync(RequiredScopes, cancellationToken)
            .ConfigureAwait(false);

        if (tokenResult.IsFailure)
        {
            return Result<CharacterRecord>.Failure(tokenResult.Error);
        }

        if (tokenResult.Value.CharacterId.HasNoValue)
        {
            return Result<CharacterRecord>.Failure("ESI_CHARACTER_ID_MISSING", "The ESI login completed, but no character identifier was returned.");
        }

        Result<IReadOnlyList<CharacterRecord>> charactersResult = await _characterRepository
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        if (charactersResult.IsFailure)
        {
            return Result<CharacterRecord>.Failure(charactersResult.Error);
        }

        CharacterId characterId = tokenResult.Value.CharacterId.Value;
        CharacterRecord? existingCharacter = charactersResult.Value.FirstOrDefault(character => character.CharacterId == characterId);
        bool hasRealDefault = charactersResult.Value.Any(character => character.IsDefault && !SpecialCharacters.IsAllSkillsV(character.CharacterId));
        bool isDefault = existingCharacter?.IsDefault ?? !hasRealDefault;

        Result<CharacterSnapshot> refreshResult = await _characterService
            .RefreshAsync(characterId, isDefault, cancellationToken)
            .ConfigureAwait(false);

        if (refreshResult.IsFailure)
        {
            return Result<CharacterRecord>.Failure(refreshResult.Error);
        }

        CharacterRecord refreshedCharacter = refreshResult.Value.Character;

        await RefreshAssetsAsync(characterId, cancellationToken).ConfigureAwait(false);
        await RefreshResearchAgentsAsync(characterId, cancellationToken).ConfigureAwait(false);
        await RefreshIndustryJobsAsync(characterId, cancellationToken).ConfigureAwait(false);

        if (isDefault)
        {
            Result<IReadOnlyList<CharacterRecord>> defaultResult = await SetDefaultAsync(characterId, cancellationToken).ConfigureAwait(false);
            if (defaultResult.IsFailure)
            {
                return Result<CharacterRecord>.Failure(defaultResult.Error);
            }

            refreshedCharacter = defaultResult.Value.First(character => character.CharacterId == characterId);
        }

        return Result<CharacterRecord>.Success(refreshedCharacter);
    }

    public async Task<Result<CharacterRecord>> RefreshAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        if (SpecialCharacters.IsAllSkillsV(characterId))
        {
            return Result<CharacterRecord>.Failure("CHARACTER_REFRESH_NOT_SUPPORTED", "The generated All Skills V character does not sync from ESI.");
        }

        Maybe<CharacterRecord> existingCharacter = await _characterRepository
            .GetByIdAsync(characterId, cancellationToken)
            .ConfigureAwait(false);

        if (existingCharacter.HasNoValue)
        {
            return Result<CharacterRecord>.Failure("CHARACTER_NOT_FOUND", $"Character {characterId.Value} was not found.");
        }

        Result<CharacterSnapshot> refreshResult = await _characterService
            .RefreshAsync(characterId, existingCharacter.Value.IsDefault, cancellationToken)
            .ConfigureAwait(false);

        if (refreshResult.IsSuccess)
        {
            await RefreshAssetsAsync(characterId, cancellationToken).ConfigureAwait(false);
            await RefreshResearchAgentsAsync(characterId, cancellationToken).ConfigureAwait(false);
            await RefreshIndustryJobsAsync(characterId, cancellationToken).ConfigureAwait(false);
        }

        return refreshResult.IsSuccess
            ? Result<CharacterRecord>.Success(refreshResult.Value.Character)
            : Result<CharacterRecord>.Failure(refreshResult.Error);
    }

    public async Task<Result<CorporationConnectionRecord>> ConnectCorporationAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        if (SpecialCharacters.IsAllSkillsV(characterId))
        {
            return Result<CorporationConnectionRecord>.Failure("CORPORATION_CONNECT_NOT_SUPPORTED", "The generated All Skills V character cannot authorize corporation access.");
        }

        Maybe<CharacterRecord> character = await _characterRepository.GetByIdAsync(characterId, cancellationToken).ConfigureAwait(false);
        if (character.HasNoValue)
        {
            return Result<CorporationConnectionRecord>.Failure("CHARACTER_NOT_FOUND", $"Character {characterId.Value} was not found.");
        }

        Maybe<EsiTokenRecord> token = await _tokenStore.ReadAsync(characterId, cancellationToken).ConfigureAwait(false);
        if (token.HasNoValue)
        {
            return Result<CorporationConnectionRecord>.Failure("TOKEN_NOT_FOUND", $"No stored ESI token was found for {character.Value.Name}. Refresh the character first.");
        }

        CorporationId corporationId = character.Value.CorporationId;
        Result<CorporationCapabilityState> capabilityResult = await _corporationCapabilityResolver
            .ResolveAsync(corporationId, characterId, token.Value.Scopes, cancellationToken)
            .ConfigureAwait(false);

        if (capabilityResult.IsFailure)
        {
            return Result<CorporationConnectionRecord>.Failure(capabilityResult.Error);
        }

        CorporationCapabilityState capability = capabilityResult.Value;
        Result<bool> capabilityValidation = ValidateCapabilityForConnect(capability);
        if (capabilityValidation.IsFailure)
        {
            return Result<CorporationConnectionRecord>.Failure(capabilityValidation.Error);
        }

        string corporationName = await ResolveCorporationNameAsync(corporationId, cancellationToken).ConfigureAwait(false);

        Result<bool> syncResult = await SyncCorporationDataAsync(corporationId, characterId, capability, cancellationToken).ConfigureAwait(false);
        if (syncResult.IsFailure)
        {
            return Result<CorporationConnectionRecord>.Failure(syncResult.Error);
        }

        CorporationConnectionRecord record = new(
            corporationId,
            corporationName,
            characterId,
            capability.HasAssetAccess,
            capability.HasIndustryJobAccess,
            capability.HasBlueprintAccess,
            capability.HasDirectorRole,
            capability.HasFactoryManagerRole);

        Result<CorporationConnectionRecord> upsertResult = await _corporationConnectionRepository
            .UpsertAsync(record, cancellationToken)
            .ConfigureAwait(false);

        return upsertResult.IsSuccess
            ? Result<CorporationConnectionRecord>.Success(record)
            : Result<CorporationConnectionRecord>.Failure(upsertResult.Error);
    }

    public async Task<Result<CorporationConnectionRecord>> RefreshCorporationAsync(CorporationId corporationId, CancellationToken cancellationToken = default)
    {
        Maybe<CorporationConnectionRecord> connection = await _corporationConnectionRepository
            .GetByIdAsync(corporationId, cancellationToken)
            .ConfigureAwait(false);

        if (connection.HasNoValue)
        {
            return Result<CorporationConnectionRecord>.Failure("CORPORATION_NOT_FOUND", $"Corporation {corporationId.Value} was not found.");
        }

        Maybe<EsiTokenRecord> token = await _tokenStore.ReadAsync(connection.Value.AuthorizedCharacterId, cancellationToken).ConfigureAwait(false);
        CorporationCapabilityState capability;

        if (token.HasValue)
        {
            Result<CorporationCapabilityState> capabilityResult = await _corporationCapabilityResolver
                .ResolveAsync(corporationId, connection.Value.AuthorizedCharacterId, token.Value.Scopes, cancellationToken)
                .ConfigureAwait(false);

            if (capabilityResult.IsFailure)
            {
                return Result<CorporationConnectionRecord>.Failure(capabilityResult.Error);
            }

            capability = capabilityResult.Value;
        }
        else
        {
            capability = new CorporationCapabilityState(false, false, false, false, false, false, false, false, false);
        }

        Result<bool> syncResult = await SyncCorporationDataAsync(corporationId, connection.Value.AuthorizedCharacterId, capability, cancellationToken).ConfigureAwait(false);
        if (syncResult.IsFailure)
        {
            return Result<CorporationConnectionRecord>.Failure(syncResult.Error);
        }

        CorporationConnectionRecord updatedConnection = connection.Value with
        {
            HasAssetAccess = capability.HasAssetAccess,
            HasIndustryJobAccess = capability.HasIndustryJobAccess,
            HasBlueprintAccess = capability.HasBlueprintAccess,
            HasDirectorRole = capability.HasDirectorRole,
            HasFactoryManagerRole = capability.HasFactoryManagerRole,
        };

        Result<CorporationConnectionRecord> upsertResult = await _corporationConnectionRepository
            .UpsertAsync(updatedConnection, cancellationToken)
            .ConfigureAwait(false);

        if (upsertResult.IsFailure)
        {
            return Result<CorporationConnectionRecord>.Failure(upsertResult.Error);
        }

        return Result<CorporationConnectionRecord>.Success(updatedConnection);
    }

    public async Task<Result<IReadOnlyList<CharacterRecord>>> SetDefaultAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<CharacterRecord>> charactersResult = await _characterRepository
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        if (charactersResult.IsFailure)
        {
            return Result<IReadOnlyList<CharacterRecord>>.Failure(charactersResult.Error);
        }

        IReadOnlyList<CharacterRecord> characters = charactersResult.Value;
        if (!characters.Any(character => character.CharacterId == characterId))
        {
            return Result<IReadOnlyList<CharacterRecord>>.Failure("CHARACTER_NOT_FOUND", $"Character {characterId.Value} was not found.");
        }

        foreach (CharacterRecord character in characters)
        {
            bool shouldBeDefault = character.CharacterId == characterId;
            if (character.IsDefault == shouldBeDefault)
            {
                continue;
            }

            Result<CharacterRecord> upsertResult = await _characterRepository
                .UpsertAsync(character with { IsDefault = shouldBeDefault }, cancellationToken)
                .ConfigureAwait(false);

            if (upsertResult.IsFailure)
            {
                return Result<IReadOnlyList<CharacterRecord>>.Failure(upsertResult.Error);
            }
        }

        return await _characterRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<IReadOnlyList<CharacterRecord>>> DeleteAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        if (SpecialCharacters.IsAllSkillsV(characterId))
        {
            return Result<IReadOnlyList<CharacterRecord>>.Failure("CHARACTER_DELETE_NOT_SUPPORTED", "The generated All Skills V character is always kept locally for calculations.");
        }

        Result<IReadOnlyList<CharacterRecord>> charactersResult = await _characterRepository
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        if (charactersResult.IsFailure)
        {
            return Result<IReadOnlyList<CharacterRecord>>.Failure(charactersResult.Error);
        }

        CharacterRecord? characterToDelete = charactersResult.Value.FirstOrDefault(character => character.CharacterId == characterId);
        if (characterToDelete is null)
        {
            return Result<IReadOnlyList<CharacterRecord>>.Failure("CHARACTER_NOT_FOUND", $"Character {characterId.Value} was not found.");
        }

        Result<IReadOnlyList<CorporationConnectionRecord>> corporationConnectionsResult = await _corporationConnectionRepository
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        if (corporationConnectionsResult.IsFailure)
        {
            return Result<IReadOnlyList<CharacterRecord>>.Failure(corporationConnectionsResult.Error);
        }

        IReadOnlyList<CorporationConnectionRecord> authorizedCorporations = corporationConnectionsResult.Value
            .Where(connection => connection.AuthorizedCharacterId == characterId)
            .ToArray();

        foreach (CorporationConnectionRecord corporation in authorizedCorporations)
        {
            Result<IReadOnlyList<CorporationConnectionRecord>> corporationDeleteResult = await DeleteCorporationAsync(corporation.CorporationId, cancellationToken)
                .ConfigureAwait(false);
            if (corporationDeleteResult.IsFailure)
            {
                return Result<IReadOnlyList<CharacterRecord>>.Failure(corporationDeleteResult.Error);
            }
        }

        Result<IReadOnlyList<IndustryJobRecord>> personalIndustryJobDeleteResult = await _industryJobRepository
            .ReplaceAsync(characterId, IndustryJobScope.Personal, [], cancellationToken)
            .ConfigureAwait(false);
        if (personalIndustryJobDeleteResult.IsFailure)
        {
            return Result<IReadOnlyList<CharacterRecord>>.Failure(personalIndustryJobDeleteResult.Error);
        }

        Result<bool> deleteResult = await _characterRepository.DeleteAsync(characterId, cancellationToken).ConfigureAwait(false);
        if (deleteResult.IsFailure)
        {
            return Result<IReadOnlyList<CharacterRecord>>.Failure(deleteResult.Error);
        }

        if (!deleteResult.Value)
        {
            return Result<IReadOnlyList<CharacterRecord>>.Failure("CHARACTER_NOT_FOUND", $"Character {characterId.Value} was not found.");
        }

        Result<bool> assetDeleteResult = await _assetRepository.DeleteByOwnerIdAsync(characterId.Value, cancellationToken).ConfigureAwait(false);
        if (assetDeleteResult.IsFailure)
        {
            return Result<IReadOnlyList<CharacterRecord>>.Failure(assetDeleteResult.Error);
        }

        Result<bool> blueprintDeleteResult = await _ownedBlueprintRepository.DeleteByUserAsync(characterId.Value, cancellationToken).ConfigureAwait(false);
        if (blueprintDeleteResult.IsFailure)
        {
            return Result<IReadOnlyList<CharacterRecord>>.Failure(blueprintDeleteResult.Error);
        }

        Result<bool> tokenDeleteResult = await _tokenStore.ClearAsync(characterId, cancellationToken).ConfigureAwait(false);
        if (tokenDeleteResult.IsFailure)
        {
            return Result<IReadOnlyList<CharacterRecord>>.Failure(tokenDeleteResult.Error);
        }

        Result<IReadOnlyList<CharacterRecord>> remainingCharacters = await _characterRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        if (remainingCharacters.IsFailure)
        {
            return Result<IReadOnlyList<CharacterRecord>>.Failure(remainingCharacters.Error);
        }

        if (characterToDelete.IsDefault && remainingCharacters.Value.Count > 0 && !remainingCharacters.Value.Any(character => character.IsDefault))
        {
            CharacterRecord replacementDefault = remainingCharacters.Value.FirstOrDefault(character => !SpecialCharacters.IsAllSkillsV(character.CharacterId))
                ?? remainingCharacters.Value[0];
            replacementDefault = replacementDefault with { IsDefault = true };
            Result<CharacterRecord> upsertResult = await _characterRepository.UpsertAsync(replacementDefault, cancellationToken).ConfigureAwait(false);
            if (upsertResult.IsFailure)
            {
                return Result<IReadOnlyList<CharacterRecord>>.Failure(upsertResult.Error);
            }

            return await _characterRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        }

        return remainingCharacters;
    }

    public async Task<Result<IReadOnlyList<CorporationConnectionRecord>>> DeleteCorporationAsync(CorporationId corporationId, CancellationToken cancellationToken = default)
    {
        Result<bool> industryJobDeleteResult = await ClearCorporationIndustryJobsAsync(corporationId, cancellationToken).ConfigureAwait(false);
        if (industryJobDeleteResult.IsFailure)
        {
            return Result<IReadOnlyList<CorporationConnectionRecord>>.Failure(industryJobDeleteResult.Error);
        }

        Result<bool> blueprintDeleteResult = await _ownedBlueprintRepository.DeleteByUserAsync(corporationId.Value, cancellationToken).ConfigureAwait(false);
        if (blueprintDeleteResult.IsFailure)
        {
            return Result<IReadOnlyList<CorporationConnectionRecord>>.Failure(blueprintDeleteResult.Error);
        }

        Result<bool> assetDeleteResult = await _assetRepository.DeleteByOwnerIdAsync(corporationId.Value, cancellationToken).ConfigureAwait(false);
        if (assetDeleteResult.IsFailure)
        {
            return Result<IReadOnlyList<CorporationConnectionRecord>>.Failure(assetDeleteResult.Error);
        }

        Result<bool> deleteResult = await _corporationConnectionRepository.DeleteAsync(corporationId, cancellationToken).ConfigureAwait(false);
        if (deleteResult.IsFailure)
        {
            return Result<IReadOnlyList<CorporationConnectionRecord>>.Failure(deleteResult.Error);
        }

        return await _corporationConnectionRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task RefreshResearchAgentsAsync(CharacterId characterId, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<ResearchAgent>> _ = await _researchAgentService
            .RefreshAsync(characterId, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task RefreshAssetsAsync(CharacterId characterId, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<EVE.IPH.Domain.Assets.Models.AssetRecord>> _ = await _characterAssetService
            .RefreshAsync(characterId, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task RefreshIndustryJobsAsync(CharacterId characterId, CancellationToken cancellationToken)
    {
        Result<EVE.IPH.Domain.Industry.Models.IndustryJobSnapshot> _ = await _characterIndustryJobService
            .RefreshAsync(characterId, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<Result<bool>> SyncCorporationDataAsync(
        CorporationId corporationId,
        CharacterId authorizedCharacterId,
        CorporationCapabilityState capability,
        CancellationToken cancellationToken)
    {
        if (capability.HasAssetAccess)
        {
            Result<IReadOnlyList<EVE.IPH.Domain.Assets.Models.AssetRecord>> assetRefresh = await _corporationAssetService
                .RefreshAsync(corporationId, authorizedCharacterId, cancellationToken)
                .ConfigureAwait(false);

            if (assetRefresh.IsFailure)
            {
                return Result<bool>.Failure(assetRefresh.Error);
            }
        }
        else
        {
            Result<bool> assetDelete = await _assetRepository.DeleteByOwnerIdAsync(corporationId.Value, cancellationToken).ConfigureAwait(false);
            if (assetDelete.IsFailure)
            {
                return Result<bool>.Failure(assetDelete.Error);
            }
        }

        if (capability.HasIndustryJobAccess)
        {
            Result<CorporationIndustryJobSnapshot> jobRefresh = await _corporationIndustryJobService
                .RefreshAsync(corporationId, cancellationToken)
                .ConfigureAwait(false);

            if (jobRefresh.IsFailure)
            {
                return Result<bool>.Failure(jobRefresh.Error);
            }
        }
        else
        {
            Result<bool> industryJobDelete = await ClearCorporationIndustryJobsAsync(corporationId, cancellationToken).ConfigureAwait(false);
            if (industryJobDelete.IsFailure)
            {
                return Result<bool>.Failure(industryJobDelete.Error);
            }
        }

        if (capability.HasBlueprintAccess)
        {
            Result<IReadOnlyList<OwnedBlueprintRecord>> blueprintRefresh = await _corporationBlueprintService
                .RefreshAsync(corporationId, authorizedCharacterId, cancellationToken)
                .ConfigureAwait(false);

            if (blueprintRefresh.IsFailure)
            {
                return Result<bool>.Failure(blueprintRefresh.Error);
            }
        }
        else
        {
            Result<bool> blueprintDelete = await _ownedBlueprintRepository.DeleteByUserAsync(corporationId.Value, cancellationToken).ConfigureAwait(false);
            if (blueprintDelete.IsFailure)
            {
                return Result<bool>.Failure(blueprintDelete.Error);
            }
        }

        return Result<bool>.Success(true);
    }

    private async Task<Result<bool>> ClearCorporationIndustryJobsAsync(CorporationId corporationId, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<CharacterRecord>> charactersResult = await _characterRepository
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        if (charactersResult.IsFailure)
        {
            return Result<bool>.Failure(charactersResult.Error);
        }

        CharacterId[] installerIds = charactersResult.Value
            .Where(character => character.CorporationId == corporationId)
            .Select(character => character.CharacterId)
            .Distinct()
            .ToArray();

        foreach (CharacterId installerId in installerIds)
        {
            Result<IReadOnlyList<IndustryJobRecord>> replaceResult = await _industryJobRepository
                .ReplaceAsync(installerId, IndustryJobScope.Corporation, [], cancellationToken)
                .ConfigureAwait(false);

            if (replaceResult.IsFailure)
            {
                return Result<bool>.Failure(replaceResult.Error);
            }
        }

        return Result<bool>.Success(true);
    }

    private static Result<bool> ValidateCapabilityForConnect(CorporationCapabilityState capability)
    {
        if (!capability.HasAnyCorporationScope)
        {
            return Result<bool>.Failure("CORPORATION_SCOPES_MISSING", "The selected character token does not include corporation assets, jobs, or blueprints scopes.");
        }

        if (!capability.HasMembershipScope)
        {
            return Result<bool>.Failure("CORPORATION_MEMBERSHIP_SCOPE_MISSING", "The selected character token must include corporation membership scope to verify corporation roles.");
        }

        if (!capability.HasAnyAccess)
        {
            return Result<bool>.Failure("CORPORATION_ROLES_MISSING", BuildMissingCapabilityMessage(capability));
        }

        return Result<bool>.Success(true);
    }

    private static string BuildMissingCapabilityMessage(CorporationCapabilityState capability)
    {
        List<string> missingRequirements = [];

        if ((capability.HasAssetScope || capability.HasBlueprintScope) && !capability.HasDirectorRole)
        {
            missingRequirements.Add("Director role for corporation assets and blueprints");
        }

        if (capability.HasIndustryJobScope && !capability.HasFactoryManagerRole)
        {
            missingRequirements.Add("Factory Manager role for corporation industry jobs");
        }

        if (missingRequirements.Count == 0)
        {
            missingRequirements.Add("the required corporation roles");
        }

        return $"The authenticated character token includes corporation scopes, but the character does not have {string.Join(" and ", missingRequirements)}.";
    }

    private async Task<string> ResolveCorporationNameAsync(CorporationId corporationId, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<EVE.IPH.Infrastructure.ESI.Models.EsiEntityName>> names = await _esiClient
            .GetNamesAsync([corporationId.Value], cancellationToken)
            .ConfigureAwait(false);

        if (names.IsSuccess)
        {
            string? corporationName = names.Value.FirstOrDefault()?.Name;
            if (!string.IsNullOrWhiteSpace(corporationName))
            {
                return corporationName;
            }
        }

        return $"Corporation {corporationId.Value}";
    }
}