using EVE.IPH.Domain.Characters.Services;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using NSubstitute;

namespace EVE.IPH.Domain.Characters.Tests.Services;

public sealed class ResearchAgentServiceTests
{
    [Fact]
    public async Task GetAsync_LoadsStoredResearchAgentsAndComputesCurrentResearchPoints()
    {
        CharacterId characterId = new(90000021);
        ICharacterResearchAgentRepository researchAgentRepository = Substitute.For<ICharacterResearchAgentRepository>();
        IResearchAgentDefinitionRepository definitionRepository = Substitute.For<IResearchAgentDefinitionRepository>();
        IItemRepository itemRepository = Substitute.For<IItemRepository>();
        ICharacterResearchAgentDataSource dataSource = Substitute.For<ICharacterResearchAgentDataSource>();
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 4, 11, 12, 0, 0, TimeSpan.Zero));

        researchAgentRepository.GetByCharacterIdAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CharacterResearchAgentRecord>>.Success([
                new CharacterResearchAgentRecord(
                    characterId,
                    3019499,
                    new TypeId(11452),
                    54.5,
                    new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero),
                    12.25),
            ]));
        definitionRepository.GetByAgentIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<long, ResearchAgentDefinitionRecord>>.Success(
                new Dictionary<long, ResearchAgentDefinitionRecord>
                {
                    [3019499] = new(3019499, "Kikko Onimaro", 54.5, 4, "Jita IV - Moon 4"),
                }));
        itemRepository.GetItemNameAsync(new TypeId(11452), Arg.Any<CancellationToken>())
            .Returns(Maybe<string>.Some("Mechanical Engineering"));

        ResearchAgentService service = new(researchAgentRepository, definitionRepository, itemRepository, dataSource, timeProvider);

        var result = await service.GetAsync(characterId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].AgentName.Should().Be("Kikko Onimaro");
        result.Value[0].Field.Should().Be("Mechanical Engineering");
        result.Value[0].CurrentResearchPoints.Should().BeApproximately((54.5 * 10) + 12.25, 0.001);
    }

    [Fact]
    public async Task RefreshAsync_PersistsAndReturnsCurrentResearchAgents()
    {
        CharacterId characterId = new(90000022);
        ICharacterResearchAgentRepository researchAgentRepository = Substitute.For<ICharacterResearchAgentRepository>();
        IResearchAgentDefinitionRepository definitionRepository = Substitute.For<IResearchAgentDefinitionRepository>();
        IItemRepository itemRepository = Substitute.For<IItemRepository>();
        ICharacterResearchAgentDataSource dataSource = Substitute.For<ICharacterResearchAgentDataSource>();
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 4, 11, 12, 0, 0, TimeSpan.Zero));
        DateTimeOffset startedAt = new(2026, 4, 10, 12, 0, 0, TimeSpan.Zero);

        dataSource.GetResearchAgentsAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CharacterResearchAgentData>>.Success([
                new CharacterResearchAgentData(3019499, new TypeId(11452), startedAt, 54.5, 12.25),
            ]));
        researchAgentRepository.ReplaceAsync(Arg.Any<CharacterId>(), Arg.Any<IReadOnlyList<CharacterResearchAgentRecord>>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<IReadOnlyList<CharacterResearchAgentRecord>>.Success(call.Arg<IReadOnlyList<CharacterResearchAgentRecord>>()));
        definitionRepository.GetByAgentIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<long, ResearchAgentDefinitionRecord>>.Success(
                new Dictionary<long, ResearchAgentDefinitionRecord>
                {
                    [3019499] = new(3019499, "Kikko Onimaro", 54.5, 4, "Jita IV - Moon 4"),
                }));
        itemRepository.GetItemNameAsync(new TypeId(11452), Arg.Any<CancellationToken>())
            .Returns(Maybe<string>.Some("Mechanical Engineering"));

        ResearchAgentService service = new(researchAgentRepository, definitionRepository, itemRepository, dataSource, timeProvider);

        var result = await service.RefreshAsync(characterId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(agent => agent.AgentId == 3019499);
        await researchAgentRepository.Received(1).ReplaceAsync(
            Arg.Is<CharacterId>(id => id == characterId),
            Arg.Is<IReadOnlyList<CharacterResearchAgentRecord>>(records => records.Count == 1 && records[0].AgentId == 3019499),
            Arg.Any<CancellationToken>());
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private readonly DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}