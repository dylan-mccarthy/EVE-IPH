using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.Domain.Assets.Services;
using EVE.IPH.Domain.Characters.Services;
using EVE.IPH.Domain.Core;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Industry.Services;
using EVE.IPH.Infrastructure.ESI;
using EVE.IPH.Infrastructure.ESI.Interfaces;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class CharacterManagementService : ICharacterManagementService
{
    private static readonly string[] RequiredScopes =
    [
        "esi-skills.read_skills.v1",
        "esi-characters.read_standings.v1",
        "esi-characters.read_agents_research.v1",
        "esi-industry.read_character_jobs.v1",
    ];

    private static readonly string[] CorporationDataScopes =
    [
        "esi-assets.read_corporation_assets",
        "esi-industry.read_corporation_jobs",
        "esi-corporations.read_blueprints",
    ];

    private readonly ICharacterRepository _characterRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly ICharacterAssetService _characterAssetService;
    private readonly ICorporationAssetService _corporationAssetService;
    private readonly ICorporationConnectionRepository _corporationConnectionRepository;
    private readonly ICharacterService _characterService;
    private readonly ICharacterIndustryJobService _characterIndustryJobService;
    private readonly ICorporationIndustryJobService _corporationIndustryJobService;
    private readonly IEsiClient _esiClient;
    private readonly IEsiInteractiveLoginService _interactiveLoginService;
    private readonly IResearchAgentService _researchAgentService;
    private readonly IEsiTokenStore _tokenStore;

    public CharacterManagementService(
        ICharacterRepository characterRepository,
        IAssetRepository assetRepository,
        ICharacterAssetService characterAssetService,
        ICorporationAssetService corporationAssetService,
        ICorporationConnectionRepository corporationConnectionRepository,
        ICharacterService characterService,
        ICharacterIndustryJobService characterIndustryJobService,
        ICorporationIndustryJobService corporationIndustryJobService,
        IEsiClient esiClient,
        IEsiInteractiveLoginService interactiveLoginService,
        IResearchAgentService researchAgentService,
        IEsiTokenStore tokenStore)
    {
        _characterRepository = characterRepository ?? throw new ArgumentNullException(nameof(characterRepository));
        _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
        _characterAssetService = characterAssetService ?? throw new ArgumentNullException(nameof(characterAssetService));
        _corporationAssetService = corporationAssetService ?? throw new ArgumentNullException(nameof(corporationAssetService));
        _corporationConnectionRepository = corporationConnectionRepository ?? throw new ArgumentNullException(nameof(corporationConnectionRepository));
        _characterService = characterService ?? throw new ArgumentNullException(nameof(characterService));
        _characterIndustryJobService = characterIndustryJobService ?? throw new ArgumentNullException(nameof(characterIndustryJobService));
        _corporationIndustryJobService = corporationIndustryJobService ?? throw new ArgumentNullException(nameof(corporationIndustryJobService));
        _esiClient = esiClient ?? throw new ArgumentNullException(nameof(esiClient));
        _interactiveLoginService = interactiveLoginService ?? throw new ArgumentNullException(nameof(interactiveLoginService));
        _researchAgentService = researchAgentService ?? throw new ArgumentNullException(nameof(researchAgentService));
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
    }

    public Task<Result<IReadOnlyList<CharacterRecord>>> GetCharactersAsync(CancellationToken cancellationToken = default) =>
        _characterRepository.GetAllAsync(cancellationToken);

    public async Task<Result<IReadOnlyList<CharacterTokenStatus>>> GetCharacterTokenStatusesAsync(CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<CharacterRecord>> charactersResult = await _characterRepository
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        if (charactersResult.IsFailure)
        {
            return Result<IReadOnlyList<CharacterTokenStatus>>.Failure(charactersResult.Error);
        }

        List<CharacterTokenStatus> statuses = [];
        DateTimeOffset now = DateTimeOffset.UtcNow;

        foreach (CharacterRecord character in charactersResult.Value)
        {
            Maybe<EsiTokenRecord> token = await _tokenStore.ReadAsync(character.CharacterId, cancellationToken).ConfigureAwait(false);
            statuses.Add(BuildTokenStatus(character.CharacterId, token, now));
        }

        return Result<IReadOnlyList<CharacterTokenStatus>>.Success(statuses);
    }

    public Task<Result<IReadOnlyList<CorporationConnectionRecord>>> GetCorporationsAsync(CancellationToken cancellationToken = default) =>
        _corporationConnectionRepository.GetAllAsync(cancellationToken);

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

        IReadOnlyList<string> scopes = token.Value.Scopes;
        bool hasAssetAccess = scopes.Contains(CorporationDataScopes[0], StringComparer.OrdinalIgnoreCase);
        bool hasIndustryJobAccess = scopes.Contains(CorporationDataScopes[1], StringComparer.OrdinalIgnoreCase);
        bool hasBlueprintAccess = scopes.Contains(CorporationDataScopes[2], StringComparer.OrdinalIgnoreCase);

        if (!hasAssetAccess && !hasIndustryJobAccess && !hasBlueprintAccess)
        {
            return Result<CorporationConnectionRecord>.Failure("CORPORATION_SCOPES_MISSING", "The selected character token does not include corporation assets, jobs, or blueprints scopes.");
        }

        CorporationId corporationId = character.Value.CorporationId;
        string corporationName = await ResolveCorporationNameAsync(corporationId, cancellationToken).ConfigureAwait(false);

        if (hasAssetAccess)
        {
            Result<IReadOnlyList<EVE.IPH.Domain.Assets.Models.AssetRecord>> assetRefresh = await _corporationAssetService
                .RefreshAsync(corporationId, characterId, cancellationToken)
                .ConfigureAwait(false);

            if (assetRefresh.IsFailure)
            {
                return Result<CorporationConnectionRecord>.Failure(assetRefresh.Error);
            }
        }

        CorporationConnectionRecord record = new(
            corporationId,
            corporationName,
            characterId,
            hasAssetAccess,
            hasIndustryJobAccess,
            hasBlueprintAccess);

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

        if (connection.Value.HasAssetAccess)
        {
            Result<IReadOnlyList<EVE.IPH.Domain.Assets.Models.AssetRecord>> assetRefresh = await _corporationAssetService
                .RefreshAsync(corporationId, connection.Value.AuthorizedCharacterId, cancellationToken)
                .ConfigureAwait(false);

            if (assetRefresh.IsFailure)
            {
                return Result<CorporationConnectionRecord>.Failure(assetRefresh.Error);
            }
        }

        return Result<CorporationConnectionRecord>.Success(connection.Value);
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

        Result<bool> deleteResult = await _characterRepository.DeleteAsync(characterId, cancellationToken).ConfigureAwait(false);
        if (deleteResult.IsFailure)
        {
            return Result<IReadOnlyList<CharacterRecord>>.Failure(deleteResult.Error);
        }

        if (!deleteResult.Value)
        {
            return Result<IReadOnlyList<CharacterRecord>>.Failure("CHARACTER_NOT_FOUND", $"Character {characterId.Value} was not found.");
        }

        Result<bool> _assetDelete = await _assetRepository.DeleteByOwnerIdAsync(characterId.Value, cancellationToken).ConfigureAwait(false);
        Result<bool> _ = await _tokenStore.ClearAsync(characterId, cancellationToken).ConfigureAwait(false);

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

    private static CharacterTokenStatus BuildTokenStatus(CharacterId characterId, Maybe<EsiTokenRecord> token, DateTimeOffset now)
    {
        if (token.HasNoValue)
        {
            return new CharacterTokenStatus(characterId, false, true, null, "No stored token. Refresh the character to re-authenticate.", []);
        }

        bool isExpired = token.Value.ExpiresAtUtc <= now;
        string statusText = isExpired
            ? $"Token expired at {token.Value.ExpiresAtUtc:yyyy-MM-dd HH:mm} UTC."
            : $"Token valid until {token.Value.ExpiresAtUtc:yyyy-MM-dd HH:mm} UTC.";

        return new CharacterTokenStatus(characterId, true, isExpired, token.Value.ExpiresAtUtc, statusText, token.Value.Scopes);
    }
}