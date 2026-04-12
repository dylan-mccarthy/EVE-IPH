using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Manufacturing.Services;
using EVE.IPH.UI.Avalonia.Services;
using NSubstitute;

namespace EVE.IPH.UI.Avalonia.Tests.Services;

public sealed class BlueprintManagementServiceTests
{
    [Fact]
    public async Task GetScreenDataAsync_MapsOwnersAndBlueprintRows()
    {
        ICharacterManagementQueryService characterManagementQueryService = Substitute.For<ICharacterManagementQueryService>();
        IOwnedBlueprintWorkflowService ownedBlueprintWorkflowService = Substitute.For<IOwnedBlueprintWorkflowService>();

        characterManagementQueryService.GetScreenDataAsync(Arg.Any<CancellationToken>())
            .Returns(Result<CharacterManagementScreenData>.Success(new CharacterManagementScreenData(
                [new CharacterManagementCharacterRow(
                    new CharacterRecord(new CharacterId(90000001), "Kara Maken", new CorporationId(98000001), Maybe<AllianceId>.None, true),
                    new CharacterTokenStatus(new CharacterId(90000001), true, false, DateTimeOffset.UtcNow.AddHours(1), "Healthy", []))],
                [new CharacterManagementCorporationRow(
                    new CorporationConnectionRecord(new CorporationId(98000001), "Acme Holdings", new CharacterId(90000001), true, false, true),
                    "Kara Maken")],
                "Loaded")));

        ownedBlueprintWorkflowService.GetBlueprintsByOwnersAsync(Arg.Any<IReadOnlyList<long>>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<OwnedBlueprintViewRecord>>.Success([
                new OwnedBlueprintViewRecord(90000001, "Kara Maken", false, 7000001, 60015068, 28607, "Character Blueprint", 1, 10, 20, -1, 1, true, true),
                new OwnedBlueprintViewRecord(98000001, "Acme Holdings", true, 7000002, 60015068, 28608, "Corporation Blueprint", 1, 8, 16, -1, 1, true, true),
            ]));

        BlueprintManagementQueryService service = new(characterManagementQueryService, ownedBlueprintWorkflowService);

        BlueprintManagementScreenData result = await service.GetScreenDataAsync();

        result.OwnerOptions.Select(option => option.DisplayName).Should().ContainInOrder("All Owners", "Kara Maken", "Acme Holdings");
        result.Blueprints.Should().HaveCount(2);
        result.StatusText.Should().Contain("Loaded owned blueprints");
    }

    [Fact]
    public async Task SaveBlueprintAsync_ForwardsToWorkflowService()
    {
        IBlueprintManagementQueryService blueprintManagementQueryService = Substitute.For<IBlueprintManagementQueryService>();
        ICharacterManagementQueryService characterManagementQueryService = Substitute.For<ICharacterManagementQueryService>();
        ICharacterManagementCommandService characterManagementCommandService = Substitute.For<ICharacterManagementCommandService>();
        IOwnedBlueprintWorkflowService ownedBlueprintWorkflowService = Substitute.For<IOwnedBlueprintWorkflowService>();
        OwnedBlueprintRecord record = new(90000001, new ItemId(7000001), 60015068, new BlueprintId(28607), "Orca Blueprint", 1, 10, 20, -1, 1, true, true);
        ownedBlueprintWorkflowService.SaveBlueprintAsync(record, Arg.Any<CancellationToken>())
            .Returns(Result<OwnedBlueprintRecord>.Success(record));

        BlueprintManagementCommandService service = new(blueprintManagementQueryService, characterManagementQueryService, characterManagementCommandService, ownedBlueprintWorkflowService);

        Result<OwnedBlueprintRecord> result = await service.SaveBlueprintAsync(record);

        result.IsSuccess.Should().BeTrue();
        await ownedBlueprintWorkflowService.Received(1).SaveBlueprintAsync(record, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_RefreshesConnectedOwnersThenReloadsBlueprints()
    {
        IBlueprintManagementQueryService blueprintManagementQueryService = Substitute.For<IBlueprintManagementQueryService>();
        ICharacterManagementQueryService characterManagementQueryService = Substitute.For<ICharacterManagementQueryService>();
        ICharacterManagementCommandService characterManagementCommandService = Substitute.For<ICharacterManagementCommandService>();
        IOwnedBlueprintWorkflowService ownedBlueprintWorkflowService = Substitute.For<IOwnedBlueprintWorkflowService>();

        CharacterRecord character = new(new CharacterId(90000001), "Kara Maken", new CorporationId(98000001), Maybe<AllianceId>.None, true);
        CorporationConnectionRecord corporation = new(new CorporationId(98000001), "Acme Holdings", character.CharacterId, true, false, true);

        characterManagementQueryService.GetScreenDataAsync(Arg.Any<CancellationToken>())
            .Returns(Result<CharacterManagementScreenData>.Success(new CharacterManagementScreenData(
                [new CharacterManagementCharacterRow(character, new CharacterTokenStatus(character.CharacterId, true, false, DateTimeOffset.UtcNow.AddHours(1), "Healthy", []))],
                [new CharacterManagementCorporationRow(corporation, "Kara Maken")],
                "Loaded")));
        characterManagementCommandService.RefreshAsync(character.CharacterId, Arg.Any<CancellationToken>())
            .Returns(Result<CharacterRecord>.Success(character));
        characterManagementCommandService.RefreshCorporationAsync(corporation.CorporationId, Arg.Any<CancellationToken>())
            .Returns(Result<CorporationConnectionRecord>.Success(corporation));
        blueprintManagementQueryService.GetScreenDataAsync(Arg.Any<CancellationToken>())
            .Returns(new BlueprintManagementScreenData([], [new BlueprintOwnerFilterOption(null, "All Owners")], "Loaded owned blueprints for the connected characters and corporations."));

        BlueprintManagementCommandService service = new(blueprintManagementQueryService, characterManagementQueryService, characterManagementCommandService, ownedBlueprintWorkflowService);

        BlueprintManagementScreenData result = await service.RefreshAsync();

        await characterManagementCommandService.Received(1).RefreshAsync(character.CharacterId, Arg.Any<CancellationToken>());
        await characterManagementCommandService.Received(1).RefreshCorporationAsync(corporation.CorporationId, Arg.Any<CancellationToken>());
        result.StatusText.Should().Contain("reloaded owned blueprints");
    }
}