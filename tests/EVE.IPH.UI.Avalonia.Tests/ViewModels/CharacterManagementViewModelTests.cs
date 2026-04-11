using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Core;
using EVE.IPH.UI.Avalonia.Services;
using EVE.IPH.UI.Avalonia.ViewModels;
using NSubstitute;

namespace EVE.IPH.UI.Avalonia.Tests.ViewModels;

public sealed class CharacterManagementViewModelTests
{
    [Fact]
    public async Task Constructor_WhenNoCharactersStored_ShowsEmptyState()
    {
        ICharacterManagementQueryService queryService = Substitute.For<ICharacterManagementQueryService>();
        ICharacterManagementCommandService commandService = Substitute.For<ICharacterManagementCommandService>();
        ConfigureLoad(queryService, Array.Empty<CharacterRecord>(), Array.Empty<CharacterTokenStatus>(), Array.Empty<CorporationConnectionRecord>());

        CharacterManagementViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        viewModel.Characters.Should().BeEmpty();
        viewModel.SelectedCharacter.Should().BeNull();
        viewModel.StatusText.Should().Contain("No characters have been connected yet");
    }

    [Fact]
    public async Task ConnectCharacterAsync_WhenAuthenticationSucceeds_ReloadsAndSelectsCharacter()
    {
        ICharacterManagementQueryService queryService = Substitute.For<ICharacterManagementQueryService>();
        ICharacterManagementCommandService commandService = Substitute.For<ICharacterManagementCommandService>();
        CharacterRecord existingCharacter = CreateCharacter(90000001, "Kara Maken", true);
        CharacterRecord newCharacter = CreateCharacter(90000002, "Sarma Velen", false);

        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(Result<CharacterManagementScreenData>.Success(CreateScreenData([existingCharacter], [CreateTokenStatus(existingCharacter.CharacterId)], Array.Empty<CorporationConnectionRecord>()))),
                Task.FromResult(Result<CharacterManagementScreenData>.Success(CreateScreenData([existingCharacter, newCharacter], [CreateTokenStatus(existingCharacter.CharacterId), CreateTokenStatus(newCharacter.CharacterId)], Array.Empty<CorporationConnectionRecord>()))));
        commandService.AuthenticateAndRefreshAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CharacterRecord>.Success(newCharacter)));

        CharacterManagementViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        await viewModel.ConnectCharacterAsync();

        viewModel.Characters.Should().HaveCount(2);
        viewModel.SelectedCharacter.Should().NotBeNull();
        viewModel.SelectedCharacter!.Character.Should().Be(newCharacter);
        viewModel.StatusText.Should().Be("Synced Sarma Velen from ESI.");
    }

    [Fact]
    public async Task Constructor_WhenOnlyPlaceholderStored_DisablesRefreshAndDelete()
    {
        ICharacterManagementQueryService queryService = Substitute.For<ICharacterManagementQueryService>();
        ICharacterManagementCommandService commandService = Substitute.For<ICharacterManagementCommandService>();
        CharacterRecord placeholder = CreateCharacter(SpecialCharacters.AllSkillsVId.Value, SpecialCharacters.AllSkillsVName, true);

        ConfigureLoad(queryService, [placeholder], [CreateTokenStatus(placeholder.CharacterId, true, false, "Synthetic placeholder")], Array.Empty<CorporationConnectionRecord>());

        CharacterManagementViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        viewModel.SelectedCharacter.Should().NotBeNull();
        viewModel.SelectedCharacter!.Character.Should().Be(placeholder);
        viewModel.CanRefreshSelectedCharacter.Should().BeFalse();
        viewModel.CanDeleteSelectedCharacter.Should().BeFalse();
        viewModel.StatusText.Should().Contain("generated All Skills V placeholder");
    }

    [Fact]
    public async Task SetSelectedCharacterDefaultAsync_WhenSuccessful_UpdatesSelection()
    {
        ICharacterManagementQueryService queryService = Substitute.For<ICharacterManagementQueryService>();
        ICharacterManagementCommandService commandService = Substitute.For<ICharacterManagementCommandService>();
        CharacterRecord currentDefault = CreateCharacter(90000001, "Kara Maken", true);
        CharacterRecord alternateCharacter = CreateCharacter(90000002, "Sarma Velen", false);
        CharacterRecord updatedDefault = alternateCharacter with { IsDefault = true };
        CharacterRecord updatedNonDefault = currentDefault with { IsDefault = false };

        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(Result<CharacterManagementScreenData>.Success(CreateScreenData([currentDefault, alternateCharacter], [CreateTokenStatus(currentDefault.CharacterId), CreateTokenStatus(alternateCharacter.CharacterId)], Array.Empty<CorporationConnectionRecord>()))),
                Task.FromResult(Result<CharacterManagementScreenData>.Success(CreateScreenData([updatedNonDefault, updatedDefault], [CreateTokenStatus(updatedNonDefault.CharacterId), CreateTokenStatus(updatedDefault.CharacterId)], Array.Empty<CorporationConnectionRecord>()))));
        commandService.SetDefaultAsync(alternateCharacter.CharacterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CharacterRecord>>.Success(new[] { updatedNonDefault, updatedDefault })));

        CharacterManagementViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;
        viewModel.SelectedCharacter = viewModel.Characters.Single(character => character.CharacterId == alternateCharacter.CharacterId);

        await viewModel.SetSelectedCharacterDefaultAsync();

        viewModel.SelectedCharacter.Should().NotBeNull();
        viewModel.SelectedCharacter!.Character.Should().Be(updatedDefault);
        viewModel.StatusText.Should().Be("Sarma Velen is now the default character.");
        viewModel.CanSetDefaultSelectedCharacter.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSelectedCharacterAsync_WhenSuccessful_RemovesCharacterAndSelectsRemainingDefault()
    {
        ICharacterManagementQueryService queryService = Substitute.For<ICharacterManagementQueryService>();
        ICharacterManagementCommandService commandService = Substitute.For<ICharacterManagementCommandService>();
        CharacterRecord deletedCharacter = CreateCharacter(90000001, "Kara Maken", true);
        CharacterRecord remainingCharacter = CreateCharacter(90000002, "Sarma Velen", true);

        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(Result<CharacterManagementScreenData>.Success(CreateScreenData([deletedCharacter, CreateCharacter(90000002, "Sarma Velen", false)], [CreateTokenStatus(deletedCharacter.CharacterId), CreateTokenStatus(remainingCharacter.CharacterId)], Array.Empty<CorporationConnectionRecord>()))),
                Task.FromResult(Result<CharacterManagementScreenData>.Success(CreateScreenData([remainingCharacter], [CreateTokenStatus(remainingCharacter.CharacterId)], Array.Empty<CorporationConnectionRecord>()))));
        commandService.DeleteAsync(deletedCharacter.CharacterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CharacterRecord>>.Success(new[] { remainingCharacter })));

        CharacterManagementViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;
        viewModel.SelectedCharacter = viewModel.Characters.Single(character => character.CharacterId == deletedCharacter.CharacterId);

        await viewModel.DeleteSelectedCharacterAsync();

        viewModel.Characters.Should().ContainSingle();
        viewModel.SelectedCharacter.Should().NotBeNull();
        viewModel.SelectedCharacter!.Character.Should().Be(remainingCharacter);
        viewModel.StatusText.Should().Be("Removed Kara Maken from the local store.");
    }

    [Fact]
    public async Task ConnectCorporationFromSelectedCharacterAsync_WhenSuccessful_LoadsCorporationAndStatus()
    {
        ICharacterManagementQueryService queryService = Substitute.For<ICharacterManagementQueryService>();
        ICharacterManagementCommandService commandService = Substitute.For<ICharacterManagementCommandService>();
        CharacterRecord character = CreateCharacter(90000001, "Kara Maken", true);
        CorporationConnectionRecord corporation = new(new CorporationId(98000001), "Acme Holdings", character.CharacterId, true, true, false);

        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(Result<CharacterManagementScreenData>.Success(CreateScreenData([character], [CreateTokenStatus(character.CharacterId)], Array.Empty<CorporationConnectionRecord>()))),
                Task.FromResult(Result<CharacterManagementScreenData>.Success(CreateScreenData([character], [CreateTokenStatus(character.CharacterId)], [corporation]))));
        commandService.ConnectCorporationAsync(character.CharacterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CorporationConnectionRecord>.Success(corporation)));

        CharacterManagementViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        await viewModel.ConnectCorporationFromSelectedCharacterAsync();

        viewModel.Corporations.Should().ContainSingle();
        viewModel.SelectedCorporation.Should().NotBeNull();
        viewModel.SelectedCorporation!.Corporation.Should().Be(corporation);
        viewModel.StatusText.Should().Be("Connected corporation Acme Holdings using Kara Maken.");
    }

    [Fact]
    public async Task Constructor_LoadsTokenStatusTextForCharacters()
    {
        ICharacterManagementQueryService queryService = Substitute.For<ICharacterManagementQueryService>();
        ICharacterManagementCommandService commandService = Substitute.For<ICharacterManagementCommandService>();
        CharacterRecord character = CreateCharacter(90000001, "Kara Maken", true);
        CharacterTokenStatus tokenStatus = CreateTokenStatus(character.CharacterId, true, true, "Token expired. Reconnect this character.");

        ConfigureLoad(queryService, [character], [tokenStatus], Array.Empty<CorporationConnectionRecord>());

        CharacterManagementViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        viewModel.Characters.Should().ContainSingle();
        viewModel.Characters[0].TokenStatusText.Should().Be("Token expired. Reconnect this character.");
    }

    private static void ConfigureLoad(
        ICharacterManagementQueryService queryService,
        IReadOnlyList<CharacterRecord> characters,
        IReadOnlyList<CharacterTokenStatus> tokenStatuses,
        IReadOnlyList<CorporationConnectionRecord> corporations)
    {
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CharacterManagementScreenData>.Success(CreateScreenData(characters, tokenStatuses, corporations))));
    }

    private static CharacterManagementScreenData CreateScreenData(
        IReadOnlyList<CharacterRecord> characters,
        IReadOnlyList<CharacterTokenStatus> tokenStatuses,
        IReadOnlyList<CorporationConnectionRecord> corporations) => new(
            characters.Select(character => new CharacterManagementCharacterRow(
                character,
                tokenStatuses.Single(status => status.CharacterId == character.CharacterId)))
                .ToArray(),
            corporations.Select(corporation => new CharacterManagementCorporationRow(corporation, "Kara Maken"))
                .ToArray(),
            BuildStatusText(characters, corporations));

    private static string BuildStatusText(
        IReadOnlyList<CharacterRecord> characters,
        IReadOnlyList<CorporationConnectionRecord> corporations)
    {
        if (characters.Count == 0)
        {
            return "No characters have been connected yet. Sign in with EVE SSO to load a character profile, skills, and standings.";
        }

        bool hasPlaceholder = characters.Any(character => SpecialCharacters.IsAllSkillsV(character.CharacterId));
        bool hasRealCharacters = characters.Any(character => !SpecialCharacters.IsAllSkillsV(character.CharacterId));

        if (hasPlaceholder && !hasRealCharacters)
        {
            return "Loaded the generated All Skills V placeholder. Connect an EVE character to sync live skills and standings.";
        }

        return hasPlaceholder
            ? $"Loaded {characters.Count} stored character(s), including the generated All Skills V placeholder, and {corporations.Count} corporation connection(s)."
            : $"Loaded {characters.Count} stored character(s) and {corporations.Count} corporation connection(s).";
    }

    private static CharacterTokenStatus CreateTokenStatus(CharacterId characterId, bool hasStoredToken = true, bool isExpired = false, string? statusText = null) => new(
        characterId,
        hasStoredToken,
        isExpired,
        DateTimeOffset.UtcNow.AddHours(1),
        statusText ?? "Token healthy.",
        []);

    private static CharacterRecord CreateCharacter(long characterId, string name, bool isDefault) => new(
        new CharacterId(characterId),
        name,
        new CorporationId(98000001),
        Maybe<AllianceId>.None,
        isDefault);
}