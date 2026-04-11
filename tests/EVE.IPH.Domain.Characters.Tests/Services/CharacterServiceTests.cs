using EVE.IPH.Domain.Characters.Services;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using NSubstitute;

namespace EVE.IPH.Domain.Characters.Tests.Services;

public sealed class CharacterServiceTests
{
    [Fact]
    public async Task RefreshAsync_PersistsProfileSkillsAndStandings()
    {
        CharacterId characterId = new(90000001);
        ICharacterRepository characterRepository = Substitute.For<ICharacterRepository>();
        ICharacterSkillRepository skillRepository = Substitute.For<ICharacterSkillRepository>();
        ICharacterStandingRepository standingRepository = Substitute.For<ICharacterStandingRepository>();
        ICharacterDataSource characterDataSource = Substitute.For<ICharacterDataSource>();

        characterDataSource.GetCharacterProfileAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Result<CharacterProfileData>.Success(new CharacterProfileData(
                characterId,
                "Capsuleer",
                new CorporationId(98000001),
                Maybe<AllianceId>.None)));
        characterDataSource.GetSkillsAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CharacterSkillData>>.Success([
                new CharacterSkillData(new TypeId(3359), "Connections", 4, 4, 45255)
            ]));
        characterDataSource.GetStandingsAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CharacterStandingData>>.Success([
                new CharacterStandingData(1000169, "corporation", "Caldari Navy", 3.25D)
            ]));

        characterRepository.UpsertAsync(Arg.Any<CharacterRecord>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<CharacterRecord>.Success(call.Arg<CharacterRecord>()));
        skillRepository.ReplaceAsync(Arg.Any<CharacterId>(), Arg.Any<IReadOnlyList<CharacterSkillRecord>>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<IReadOnlyList<CharacterSkillRecord>>.Success(call.Arg<IReadOnlyList<CharacterSkillRecord>>()));
        standingRepository.ReplaceAsync(Arg.Any<CharacterId>(), Arg.Any<IReadOnlyList<CharacterStandingRecord>>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<IReadOnlyList<CharacterStandingRecord>>.Success(call.Arg<IReadOnlyList<CharacterStandingRecord>>()));

        CharacterService service = new(characterRepository, skillRepository, standingRepository, characterDataSource);

        Result<Models.CharacterSnapshot> result = await service.RefreshAsync(characterId, isDefault: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.Character.Name.Should().Be("Capsuleer");
        result.Value.Character.IsDefault.Should().BeTrue();
        result.Value.Skills.Should().ContainSingle(skill => skill.Name == "Connections");
        result.Value.Standings.Should().ContainSingle(standing => standing.NpcName == "Caldari Navy");
        await characterRepository.Received(1).UpsertAsync(Arg.Is<CharacterRecord>(record => record.CharacterId == characterId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WhenCharacterExists_LoadsSnapshotFromRepositories()
    {
        CharacterId characterId = new(90000001);
        ICharacterRepository characterRepository = Substitute.For<ICharacterRepository>();
        ICharacterSkillRepository skillRepository = Substitute.For<ICharacterSkillRepository>();
        ICharacterStandingRepository standingRepository = Substitute.For<ICharacterStandingRepository>();
        ICharacterDataSource characterDataSource = Substitute.For<ICharacterDataSource>();

        characterRepository.GetByIdAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Maybe<CharacterRecord>.Some(new CharacterRecord(
                characterId,
                "Capsuleer",
                new CorporationId(98000001),
                Maybe<AllianceId>.None,
                true)));
        skillRepository.GetByCharacterIdAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CharacterSkillRecord>>.Success([
                new CharacterSkillRecord(characterId, new TypeId(3446), "Broker Relations", 5, 5, 256000, false, 0)
            ]));
        standingRepository.GetByCharacterIdAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CharacterStandingRecord>>.Success([
                new CharacterStandingRecord(characterId, 1000169, "corporation", "Caldari Navy", 3.25D)
            ]));

        CharacterService service = new(characterRepository, skillRepository, standingRepository, characterDataSource);

        Result<Models.CharacterSnapshot> result = await service.GetAsync(characterId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Character.IsDefault.Should().BeTrue();
        result.Value.Skills[0].Name.Should().Be("Broker Relations");
        result.Value.Standings[0].NpcId.Should().Be(1000169);
    }

    [Fact]
    public async Task GetAsync_WhenCharacterIsMissing_ReturnsFailure()
    {
        CharacterId characterId = new(90000001);
        ICharacterRepository characterRepository = Substitute.For<ICharacterRepository>();
        ICharacterSkillRepository skillRepository = Substitute.For<ICharacterSkillRepository>();
        ICharacterStandingRepository standingRepository = Substitute.For<ICharacterStandingRepository>();
        ICharacterDataSource characterDataSource = Substitute.For<ICharacterDataSource>();

        characterRepository.GetByIdAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Maybe<CharacterRecord>.None);

        CharacterService service = new(characterRepository, skillRepository, standingRepository, characterDataSource);

        Result<Models.CharacterSnapshot> result = await service.GetAsync(characterId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CHARACTER_NOT_FOUND");
    }
}