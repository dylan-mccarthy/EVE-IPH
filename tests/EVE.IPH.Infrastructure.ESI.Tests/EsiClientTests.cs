using System.Net;
using System.Text;
using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.Infrastructure.ESI.Tests;

public sealed class EsiClientTests
{
    [Fact]
    public async Task GetCharacterProfileAsync_MapsPayloadToDomainModel()
    {
        const string payload = """
            {
              "name": "Capsuleer",
              "corporation_id": 98000001,
              "alliance_id": 99000001
            }
            """;

        EsiClient client = CreateClient(payload);

        var result = await client.GetCharacterProfileAsync(new CharacterId(12345));

        result.IsSuccess.Should().BeTrue();
        result.Value.CharacterId.Value.Should().Be(12345);
        result.Value.Name.Should().Be("Capsuleer");
        result.Value.CorporationId.Value.Should().Be(98000001);
        result.Value.AllianceId.HasValue.Should().BeTrue();
        result.Value.AllianceId.Value.Value.Should().Be(99000001);
    }

    [Fact]
    public async Task GetSkillsAsync_MapsSkillList()
    {
        const string payload = """
            {
              "skills": [
                {
                  "skill_id": 3380,
                  "active_skill_level": 5,
                  "trained_skill_level": 5,
                  "skillpoints_in_skill": 256000
                }
              ]
            }
            """;

        EsiClient client = CreateClient(payload);

        var result = await client.GetSkillsAsync(new CharacterId(12345));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].SkillTypeId.Value.Should().Be(3380);
        result.Value[0].ActiveSkillLevel.Should().Be(5);
        result.Value[0].SkillPointsInSkill.Should().Be(256000);
    }

    [Fact]
    public async Task GetStandingsAsync_OnHttpFailure_ReturnsFailureResult()
    {
        HttpClient httpClient = new(new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("upstream failure", Encoding.UTF8, "text/plain")
        }))
        {
            BaseAddress = new Uri("https://esi.evetech.net/latest/")
        };

        EsiClient client = new(httpClient);

        var result = await client.GetStandingsAsync(new CharacterId(12345));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ESI_502");
        result.Error.Message.Should().Contain("upstream failure");
    }

        [Fact]
        public async Task GetResearchAgentsAsync_MapsResearchAgentList()
        {
                const string payload = """
                        [
                            {
                                "agent_id": 3019499,
                                "skill_type_id": 11452,
                                "started_at": "2026-04-01T12:00:00Z",
                                "points_per_day": 54.5,
                                "remainder_points": 12.25
                            }
                        ]
                        """;

                EsiClient client = CreateClient(payload);

                var result = await client.GetResearchAgentsAsync(new CharacterId(12345));

                result.IsSuccess.Should().BeTrue();
                result.Value.Should().ContainSingle();
                result.Value[0].AgentId.Should().Be(3019499);
                result.Value[0].SkillTypeId.Value.Should().Be(11452);
                result.Value[0].PointsPerDay.Should().Be(54.5);
                result.Value[0].RemainderPoints.Should().Be(12.25);
        }

    [Fact]
    public async Task GetNamesAsync_PostsIdsAndMapsPayload()
    {
        RecordingHandler handler = new(request =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.PathAndQuery.Should().Be("/latest/universe/names/?datasource=tranquility");

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    [
                      {
                        "id": 500001,
                        "category": "faction",
                        "name": "Amarr Empire"
                      }
                    ]
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        });

        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://esi.evetech.net/latest/")
        };

        EsiClient client = new(httpClient);

        var result = await client.GetNamesAsync([500001]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].Id.Should().Be(500001);
        result.Value[0].Name.Should().Be("Amarr Empire");
    }

    private static EsiClient CreateClient(string payload)
    {
        HttpClient httpClient = new(new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        }))
        {
            BaseAddress = new Uri("https://esi.evetech.net/latest/")
        };

        return new EsiClient(httpClient);
    }
}