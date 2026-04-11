using EVE.IPH.Domain.Core;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class CharacterManagementQueryService(
    ICharacterRepository characterRepository,
    ICorporationConnectionRepository corporationConnectionRepository,
    IEsiTokenStore tokenStore) : ICharacterManagementQueryService
{
    private readonly ICharacterRepository _characterRepository = characterRepository ?? throw new ArgumentNullException(nameof(characterRepository));
    private readonly ICorporationConnectionRepository _corporationConnectionRepository = corporationConnectionRepository ?? throw new ArgumentNullException(nameof(corporationConnectionRepository));
    private readonly IEsiTokenStore _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));

    public async Task<Result<CharacterManagementScreenData>> GetScreenDataAsync(CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<CharacterRecord>> charactersResult = await _characterRepository
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        if (charactersResult.IsFailure)
        {
            return Result<CharacterManagementScreenData>.Failure(charactersResult.Error);
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        List<CharacterManagementCharacterRow> characters = [];

        foreach (CharacterRecord character in charactersResult.Value)
        {
            Maybe<EsiTokenRecord> token = await _tokenStore.ReadAsync(character.CharacterId, cancellationToken).ConfigureAwait(false);
            characters.Add(new CharacterManagementCharacterRow(character, BuildTokenStatus(character.CharacterId, token, now)));
        }

        Result<IReadOnlyList<CorporationConnectionRecord>> corporationsResult = await _corporationConnectionRepository
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        if (corporationsResult.IsFailure)
        {
            return Result<CharacterManagementScreenData>.Failure(corporationsResult.Error);
        }

        Dictionary<long, string> characterNames = characters.ToDictionary(character => character.Character.CharacterId.Value, character => character.Character.Name);
        IReadOnlyList<CharacterManagementCorporationRow> corporations = corporationsResult.Value
            .Select(corporation => new CharacterManagementCorporationRow(
                corporation,
                characterNames.GetValueOrDefault(corporation.AuthorizedCharacterId.Value, $"Character {corporation.AuthorizedCharacterId.Value}")))
            .ToArray();

        string? tokenStatusError = characters.Any(character => !character.TokenStatus.HasStoredToken)
            ? null
            : null;

        return Result<CharacterManagementScreenData>.Success(new CharacterManagementScreenData(
            characters,
            corporations,
            BuildStatusText(characters, corporations, tokenStatusError)));
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

    private static string BuildStatusText(
        IReadOnlyList<CharacterManagementCharacterRow> characters,
        IReadOnlyList<CharacterManagementCorporationRow> corporations,
        string? tokenStatusError)
    {
        if (characters.Count == 0)
        {
            return "No characters have been connected yet. Sign in with EVE SSO to load a character profile, skills, and standings.";
        }

        bool hasPlaceholder = characters.Any(character => SpecialCharacters.IsAllSkillsV(character.Character.CharacterId));
        bool hasRealCharacters = characters.Any(character => !SpecialCharacters.IsAllSkillsV(character.Character.CharacterId));

        if (hasPlaceholder && !hasRealCharacters)
        {
            return "Loaded the generated All Skills V placeholder. Connect an EVE character to sync live skills and standings.";
        }

        string summary = hasPlaceholder
            ? $"Loaded {characters.Count} stored character(s), including the generated All Skills V placeholder, and {corporations.Count} corporation connection(s)."
            : $"Loaded {characters.Count} stored character(s) and {corporations.Count} corporation connection(s).";

        return string.IsNullOrWhiteSpace(tokenStatusError)
            ? summary
            : $"{summary} Token status warning: {tokenStatusError}";
    }
}