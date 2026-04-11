using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.Domain.Core;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Repositories.App;
using EVE.IPH.UI.Avalonia.Services;
using NSubstitute;

namespace EVE.IPH.UI.Avalonia.Tests.Services;

public sealed class AssetsScreenServiceTests
{
    [Fact]
    public async Task GetScreenDataAsync_MapsOwnerOptionsFromCharacters()
    {
        IAssetReadRepository assetReadRepository = Substitute.For<IAssetReadRepository>();
        ICharacterManagementQueryService characterManagementQueryService = Substitute.For<ICharacterManagementQueryService>();

        assetReadRepository.GetHydratedAssetsAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<AssetScreenRecord>>.Success([
                new AssetScreenRecord(1001, 1, 6000001, 101, 12, 11, true, false, string.Empty, "Heavy Water", "Ice Products", "Material", "Jita 4-4", "Item Hangar", false, 1),
                new AssetScreenRecord(1002, 2, 6000001, 102, 1, 4, true, true, string.Empty, "Vargur Blueprint", "Blueprints", "Blueprint", "Jita 4-4", "Item Hangar", false, 1),
            ]));
        characterManagementQueryService.GetScreenDataAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CharacterManagementScreenData>.Success(CreateScreenData(
                [
                    new CharacterRecord(new CharacterId(1001), "Kara Maken", new CorporationId(1), Maybe<AllianceId>.None, true),
                    new CharacterRecord(new CharacterId(1002), "Mina Kall", new CorporationId(2), Maybe<AllianceId>.None, false),
                ],
                Array.Empty<CorporationConnectionRecord>()))));

        AssetsQueryService service = new(assetReadRepository, characterManagementQueryService);

        AssetsScreenData result = await service.GetScreenDataAsync();

        result.Assets.Should().HaveCount(2);
        result.OwnerOptions.Select(option => option.DisplayName).Should().ContainInOrder("All Owners", "Kara Maken", "Mina Kall");
    }

    [Fact]
    public async Task RefreshAsync_RefreshesAllRealCharactersAndSkipsPlaceholder()
    {
        IAssetsQueryService assetsQueryService = Substitute.For<IAssetsQueryService>();
        ICharacterManagementQueryService characterManagementQueryService = Substitute.For<ICharacterManagementQueryService>();
        ICharacterManagementCommandService characterManagementCommandService = Substitute.For<ICharacterManagementCommandService>();

        CharacterRecord placeholder = new(SpecialCharacters.AllSkillsVId, SpecialCharacters.AllSkillsVName, SpecialCharacters.PlaceholderCorporationId, Maybe<AllianceId>.None, false);
        CharacterRecord kara = new(new CharacterId(1001), "Kara Maken", new CorporationId(1), Maybe<AllianceId>.None, true);
        CharacterRecord mina = new(new CharacterId(1002), "Mina Kall", new CorporationId(2), Maybe<AllianceId>.None, false);

        characterManagementQueryService.GetScreenDataAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CharacterManagementScreenData>.Success(CreateScreenData([placeholder, kara, mina], Array.Empty<CorporationConnectionRecord>()))));
        characterManagementCommandService.RefreshAsync(kara.CharacterId, Arg.Any<CancellationToken>())
            .Returns(Result<CharacterRecord>.Success(kara));
        characterManagementCommandService.RefreshAsync(mina.CharacterId, Arg.Any<CancellationToken>())
            .Returns(Result<CharacterRecord>.Success(mina));
        assetsQueryService.GetScreenDataAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AssetsScreenData([], [new AssetOwnerFilterOption(null, "All Owners")], "Loaded synced asset records from the local SQLite store.")));

        AssetsCommandService service = new(assetsQueryService, characterManagementQueryService, characterManagementCommandService);

        AssetsScreenData result = await service.RefreshAsync();

        result.StatusText.Should().Be("Refreshed assets for 2 connected characters.");
        await characterManagementCommandService.Received(1).RefreshAsync(kara.CharacterId, Arg.Any<CancellationToken>());
        await characterManagementCommandService.Received(1).RefreshAsync(mina.CharacterId, Arg.Any<CancellationToken>());
        await characterManagementCommandService.DidNotReceive().RefreshAsync(SpecialCharacters.AllSkillsVId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetScreenDataAsync_IncludesCorporationOwnersInOwnerOptions()
    {
        IAssetReadRepository assetReadRepository = Substitute.For<IAssetReadRepository>();
        ICharacterManagementQueryService characterManagementQueryService = Substitute.For<ICharacterManagementQueryService>();

        assetReadRepository.GetHydratedAssetsAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<AssetScreenRecord>>.Success([
                new AssetScreenRecord(98000001, 1, 6000001, 101, 12, 11, true, false, string.Empty, "Heavy Water", "Ice Products", "Material", "Jita 4-4", "Item Hangar", false, 1),
            ]));
        characterManagementQueryService.GetScreenDataAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CharacterManagementScreenData>.Success(CreateScreenData(
                Array.Empty<CharacterRecord>(),
                [new CorporationConnectionRecord(new CorporationId(98000001), "Acme Holdings", new CharacterId(1001), true, false, false)]))));

        AssetsQueryService service = new(assetReadRepository, characterManagementQueryService);

        AssetsScreenData result = await service.GetScreenDataAsync();

        result.OwnerOptions.Select(option => option.DisplayName).Should().ContainInOrder("All Owners", "Acme Holdings");
    }

    private static CharacterManagementScreenData CreateScreenData(
        IReadOnlyList<CharacterRecord> characters,
        IReadOnlyList<CorporationConnectionRecord> corporations) => new(
            characters.Select(character => new CharacterManagementCharacterRow(
                character,
                new CharacterTokenStatus(character.CharacterId, true, false, DateTimeOffset.UtcNow.AddHours(1), "Token healthy.", [])))
                .ToArray(),
            corporations.Select(corporation => new CharacterManagementCorporationRow(corporation, "Kara Maken"))
                .ToArray(),
            "Loaded stored characters from the local SQLite store.");
}