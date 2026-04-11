using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Interfaces;
using EVE.IPH.Infrastructure.ESI.Models;
using NSubstitute;

namespace EVE.IPH.Infrastructure.ESI.Tests;

public sealed class EsiCharacterDataSourceTests
{
    [Fact]
    public async Task GetSkillsAsync_UsesItemRepositoryNamesAndFallsBackToTypeId()
    {
        IEsiClient esiClient = Substitute.For<IEsiClient>();
        IItemRepository itemRepository = Substitute.For<IItemRepository>();
        CharacterId characterId = new(100_000_401);

        esiClient.GetSkillsAsync(characterId, Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<EsiSkill>>.Success(
        [
            new EsiSkill(new TypeId(3380), 5, 4, 256000),
            new EsiSkill(new TypeId(3402), 3, 3, 8000),
        ]));

        itemRepository.GetItemNameAsync(new TypeId(3380), Arg.Any<CancellationToken>()).Returns(Maybe<string>.Some("Industry"));
        itemRepository.GetItemNameAsync(new TypeId(3402), Arg.Any<CancellationToken>()).Returns(Maybe<string>.None);

        EsiCharacterDataSource sut = new(esiClient, itemRepository);

        var result = await sut.GetSkillsAsync(characterId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(skill => skill.SkillTypeId.Value == 3380 && skill.Name == "Industry");
        result.Value.Should().Contain(skill => skill.SkillTypeId.Value == 3402 && skill.Name == "3402");
    }

    [Fact]
    public async Task GetStandingsAsync_ResolvesNamesAndMapsLegacyStandingTypes()
    {
        IEsiClient esiClient = Substitute.For<IEsiClient>();
        IItemRepository itemRepository = Substitute.For<IItemRepository>();
        CharacterId characterId = new(100_000_402);

        esiClient.GetStandingsAsync(characterId, Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<EsiStanding>>.Success(
        [
            new EsiStanding(500001, "faction", 2.4),
            new EsiStanding(500002, "npc_corp", 6.3),
            new EsiStanding(3008416, "agents", -1.1),
        ]));

        esiClient.GetNamesAsync(Arg.Any<IReadOnlyList<long>>(), Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<EsiEntityName>>.Success(
        [
            new EsiEntityName(500001, "faction", "Amarr Empire"),
            new EsiEntityName(500002, "corporation", "Caldari Navy"),
            new EsiEntityName(3008416, "agent", "Important Agent"),
        ]));

        EsiCharacterDataSource sut = new(esiClient, itemRepository);

        var result = await sut.GetStandingsAsync(characterId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(standing => standing.NpcId == 500001 && standing.NpcType == "Faction" && standing.NpcName == "Amarr Empire");
        result.Value.Should().Contain(standing => standing.NpcId == 500002 && standing.NpcType == "Corporation" && standing.NpcName == "Caldari Navy");
        result.Value.Should().Contain(standing => standing.NpcId == 3008416 && standing.NpcType == "Agent" && standing.NpcName == "Important Agent");
    }

    [Fact]
    public async Task GetStandingsAsync_WhenNameLookupFails_ReturnsFailure()
    {
        IEsiClient esiClient = Substitute.For<IEsiClient>();
        IItemRepository itemRepository = Substitute.For<IItemRepository>();
        CharacterId characterId = new(100_000_403);

        esiClient.GetStandingsAsync(characterId, Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<EsiStanding>>.Success(
        [
            new EsiStanding(500001, "faction", 2.4),
        ]));

        esiClient.GetNamesAsync(Arg.Any<IReadOnlyList<long>>(), Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<EsiEntityName>>.Failure("ESI_500", "lookup failed"));

        EsiCharacterDataSource sut = new(esiClient, itemRepository);

        var result = await sut.GetStandingsAsync(characterId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ESI_500");
    }
}