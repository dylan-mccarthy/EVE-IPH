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
        ICharacterManagementService service = Substitute.For<ICharacterManagementService>();
        ConfigureLoad(service, Array.Empty<CharacterRecord>(), Array.Empty<CharacterTokenStatus>(), Array.Empty<CorporationConnectionRecord>());

        CharacterManagementViewModel viewModel = new(service);
        await viewModel.LoadTask;

        viewModel.Characters.Should().BeEmpty();
        viewModel.SelectedCharacter.Should().BeNull();
        viewModel.StatusText.Should().Contain("No characters have been connected yet");
    }

    [Fact]
    public async Task ConnectCharacterAsync_WhenAuthenticationSucceeds_ReloadsAndSelectsCharacter()
    {
        ICharacterManagementService service = Substitute.For<ICharacterManagementService>();
        CharacterRecord existingCharacter = CreateCharacter(90000001, "Kara Maken", true);
        CharacterRecord newCharacter = CreateCharacter(90000002, "Sarma Velen", false);

        service.GetCharactersAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(Result<IReadOnlyList<CharacterRecord>>.Success(new[] { existingCharacter })),
                Task.FromResult(Result<IReadOnlyList<CharacterRecord>>.Success(new[] { existingCharacter, newCharacter })));
        service.GetCharacterTokenStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(Result<IReadOnlyList<CharacterTokenStatus>>.Success([CreateTokenStatus(existingCharacter.CharacterId)])),
                Task.FromResult(Result<IReadOnlyList<CharacterTokenStatus>>.Success([CreateTokenStatus(existingCharacter.CharacterId), CreateTokenStatus(newCharacter.CharacterId)])));
        service.GetCorporationsAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(Result<IReadOnlyList<CorporationConnectionRecord>>.Success(Array.Empty<CorporationConnectionRecord>())),
                Task.FromResult(Result<IReadOnlyList<CorporationConnectionRecord>>.Success(Array.Empty<CorporationConnectionRecord>())));
        service.AuthenticateAndRefreshAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CharacterRecord>.Success(newCharacter)));

        CharacterManagementViewModel viewModel = new(service);
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
        ICharacterManagementService service = Substitute.For<ICharacterManagementService>();
        CharacterRecord placeholder = CreateCharacter(SpecialCharacters.AllSkillsVId.Value, SpecialCharacters.AllSkillsVName, true);

        ConfigureLoad(service, [placeholder], [CreateTokenStatus(placeholder.CharacterId, true, false, "Synthetic placeholder")], Array.Empty<CorporationConnectionRecord>());

        CharacterManagementViewModel viewModel = new(service);
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
        ICharacterManagementService service = Substitute.For<ICharacterManagementService>();
        CharacterRecord currentDefault = CreateCharacter(90000001, "Kara Maken", true);
        CharacterRecord alternateCharacter = CreateCharacter(90000002, "Sarma Velen", false);
        CharacterRecord updatedDefault = alternateCharacter with { IsDefault = true };
        CharacterRecord updatedNonDefault = currentDefault with { IsDefault = false };

        ConfigureLoad(service, [currentDefault, alternateCharacter], [CreateTokenStatus(currentDefault.CharacterId), CreateTokenStatus(alternateCharacter.CharacterId)], Array.Empty<CorporationConnectionRecord>());
        service.SetDefaultAsync(alternateCharacter.CharacterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CharacterRecord>>.Success(new[] { updatedNonDefault, updatedDefault })));

        CharacterManagementViewModel viewModel = new(service);
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
        ICharacterManagementService service = Substitute.For<ICharacterManagementService>();
        CharacterRecord deletedCharacter = CreateCharacter(90000001, "Kara Maken", true);
        CharacterRecord remainingCharacter = CreateCharacter(90000002, "Sarma Velen", true);

        service.GetCharactersAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CharacterRecord>>.Success(new[] { deletedCharacter, CreateCharacter(90000002, "Sarma Velen", false) })));
        service.GetCharacterTokenStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(Result<IReadOnlyList<CharacterTokenStatus>>.Success([CreateTokenStatus(deletedCharacter.CharacterId), CreateTokenStatus(remainingCharacter.CharacterId)])),
                Task.FromResult(Result<IReadOnlyList<CharacterTokenStatus>>.Success([CreateTokenStatus(remainingCharacter.CharacterId)])));
        service.GetCorporationsAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(Result<IReadOnlyList<CorporationConnectionRecord>>.Success(Array.Empty<CorporationConnectionRecord>())),
                Task.FromResult(Result<IReadOnlyList<CorporationConnectionRecord>>.Success(Array.Empty<CorporationConnectionRecord>())));
        service.DeleteAsync(deletedCharacter.CharacterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CharacterRecord>>.Success(new[] { remainingCharacter })));

        CharacterManagementViewModel viewModel = new(service);
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
        ICharacterManagementService service = Substitute.For<ICharacterManagementService>();
        CharacterRecord character = CreateCharacter(90000001, "Kara Maken", true);
        CorporationConnectionRecord corporation = new(new CorporationId(98000001), "Acme Holdings", character.CharacterId, true, true, false);

        service.GetCharactersAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(Result<IReadOnlyList<CharacterRecord>>.Success([character])),
                Task.FromResult(Result<IReadOnlyList<CharacterRecord>>.Success([character])));
        service.GetCharacterTokenStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(Result<IReadOnlyList<CharacterTokenStatus>>.Success([CreateTokenStatus(character.CharacterId)])),
                Task.FromResult(Result<IReadOnlyList<CharacterTokenStatus>>.Success([CreateTokenStatus(character.CharacterId)])));
        service.GetCorporationsAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(Result<IReadOnlyList<CorporationConnectionRecord>>.Success(Array.Empty<CorporationConnectionRecord>())),
                Task.FromResult(Result<IReadOnlyList<CorporationConnectionRecord>>.Success([corporation])));
        service.ConnectCorporationAsync(character.CharacterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CorporationConnectionRecord>.Success(corporation)));

        CharacterManagementViewModel viewModel = new(service);
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
        ICharacterManagementService service = Substitute.For<ICharacterManagementService>();
        CharacterRecord character = CreateCharacter(90000001, "Kara Maken", true);
        CharacterTokenStatus tokenStatus = CreateTokenStatus(character.CharacterId, true, true, "Token expired. Reconnect this character.");

        ConfigureLoad(service, [character], [tokenStatus], Array.Empty<CorporationConnectionRecord>());

        CharacterManagementViewModel viewModel = new(service);
        await viewModel.LoadTask;

        viewModel.Characters.Should().ContainSingle();
        viewModel.Characters[0].TokenStatusText.Should().Be("Token expired. Reconnect this character.");
    }

    private static void ConfigureLoad(
        ICharacterManagementService service,
        IReadOnlyList<CharacterRecord> characters,
        IReadOnlyList<CharacterTokenStatus> tokenStatuses,
        IReadOnlyList<CorporationConnectionRecord> corporations)
    {
        service.GetCharactersAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CharacterRecord>>.Success(characters)));
        service.GetCharacterTokenStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CharacterTokenStatus>>.Success(tokenStatuses)));
        service.GetCorporationsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CorporationConnectionRecord>>.Success(corporations)));
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