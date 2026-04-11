using System.Net;
using System.Text;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Interfaces;
using NSubstitute;

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
        public async Task GetCharacterAssetsAsync_MapsAssetList()
        {
                const string payload = """
                        [
                            {
                                "item_id": 1001,
                                "location_id": 60003760,
                                "type_id": 35,
                                "quantity": 100,
                                "location_flag": 4,
                                "is_singleton": true,
                                "is_blueprint_copy": true,
                                "name": "Ammo Copy"
                            }
                        ]
                        """;

                EsiClient client = CreateClient(payload);

                var result = await client.GetCharacterAssetsAsync(new CharacterId(12345));

                result.IsSuccess.Should().BeTrue();
                result.Value.Should().ContainSingle();
                result.Value[0].OwnerId.Should().Be(12345);
                result.Value[0].ItemId.Should().Be(1001);
                result.Value[0].TypeId.Value.Should().Be(35);
                result.Value[0].IsBlueprintCopy.Should().BeTrue();
                result.Value[0].ItemName.Should().Be("Ammo Copy");
        }

        [Fact]
        public async Task GetCorporationRolesAsync_FiltersAndFlattensRolesForAuthenticatedCharacter()
        {
                const string payload = """
                        [
                            {
                                "character_id": 12345,
                                "roles": ["Director"],
                                "roles_at_base": ["Factory_Manager"],
                                "roles_at_hq": [],
                                "roles_at_other": []
                            },
                            {
                                "character_id": 54321,
                                "roles": ["Director"],
                                "roles_at_base": [],
                                "roles_at_hq": [],
                                "roles_at_other": []
                            }
                        ]
                        """;

                EsiClient client = CreateClient(payload);

                var result = await client.GetCorporationRolesAsync(new CorporationId(98000001), new CharacterId(12345));

                result.IsSuccess.Should().BeTrue();
                result.Value.Should().BeEquivalentTo(["Director", "Factory_Manager"]);
        }

        [Fact]
        public async Task GetCorporationBlueprintsAsync_MapsBlueprintList()
        {
                const string payload = """
                        [
                            {
                                "item_id": 7000001,
                                "location_id": 60015068,
                                "type_id": 28607,
                                "quantity": -1,
                                "time_efficiency": 14,
                                "material_efficiency": 9,
                                "runs": -1
                            }
                        ]
                        """;

                EsiClient client = CreateClient(payload);

                var result = await client.GetCorporationBlueprintsAsync(new CorporationId(98000001), new CharacterId(12345));

                result.IsSuccess.Should().BeTrue();
                result.Value.Should().ContainSingle();
                result.Value[0].OwnerId.Should().Be(98000001);
                result.Value[0].ItemId.Value.Should().Be(7000001);
                result.Value[0].BlueprintId.Value.Should().Be(28607);
                result.Value[0].Me.Should().Be(9);
                result.Value[0].Te.Should().Be(14);
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

        EsiClient client = new(httpClient, CreateTokenProvider());

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

        EsiClient client = new(httpClient, CreateTokenProvider());

        var result = await client.GetNamesAsync([500001]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].Id.Should().Be(500001);
        result.Value[0].Name.Should().Be("Amarr Empire");
    }

        [Fact]
        public async Task GetCharacterIndustryJobsAsync_MapsIndustryJobs()
        {
                const string payload = """
                        [
                            {
                                "job_id": 42,
                                "installer_id": 12345,
                                "facility_id": 60015068,
                                "location_id": 60015068,
                                "activity_id": 1,
                                "blueprint_id": 9001,
                                "blueprint_type_id": 28607,
                                "blueprint_location_id": 60015068,
                                "output_location_id": 60015068,
                                "runs": 2,
                                "cost": 1550000.5,
                                "licensed_runs": 10,
                                "probability": 1.0,
                                "product_type_id": 19720,
                                "status": "active",
                                "duration": 3600,
                                "start_date": "2026-04-10T10:00:00Z",
                                "end_date": "2026-04-10T11:00:00Z",
                                "completed_character_id": 12345,
                                "successful_runs": 0
                            }
                        ]
                        """;

                EsiClient client = CreateClient(payload);

                var result = await client.GetCharacterIndustryJobsAsync(new CharacterId(12345));

                result.IsSuccess.Should().BeTrue();
                result.Value.Should().ContainSingle();
                result.Value[0].JobId.Should().Be(42);
                result.Value[0].Scope.Should().Be(EVE.IPH.Domain.Core.Interfaces.IndustryJobScope.Personal);
                result.Value[0].ProductTypeId.HasValue.Should().BeTrue();
                result.Value[0].ProductTypeId.Value.Value.Should().Be(19720);
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

        return new EsiClient(httpClient, CreateTokenProvider());
    }

    private static IEsiTokenProvider CreateTokenProvider()
    {
        IEsiTokenProvider tokenProvider = NSubstitute.Substitute.For<IEsiTokenProvider>();
        tokenProvider.GetAccessTokenAsync(Arg.Any<CharacterId>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<EsiAccessToken>.Success(new EsiAccessToken(
                "access-token",
                "refresh-token",
                DateTimeOffset.UtcNow.AddMinutes(20),
                [],
                Maybe<CharacterId>.Some(call.Arg<CharacterId>()))));
        tokenProvider.RefreshAccessTokenAsync(Arg.Any<CharacterId>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<EsiAccessToken>.Success(new EsiAccessToken(
                "refreshed-token",
                "refresh-token",
                DateTimeOffset.UtcNow.AddMinutes(20),
                [],
                Maybe<CharacterId>.Some(call.Arg<CharacterId>()))));
        tokenProvider.GetAccessTokenAsync(Arg.Any<CancellationToken>())
            .Returns(Result<EsiAccessToken>.Success(new EsiAccessToken(
                "access-token",
                "refresh-token",
                DateTimeOffset.UtcNow.AddMinutes(20),
                [],
                Maybe<CharacterId>.Some(new CharacterId(12345)))));
        tokenProvider.RefreshAccessTokenAsync(Arg.Any<CancellationToken>())
            .Returns(Result<EsiAccessToken>.Success(new EsiAccessToken(
                "refreshed-token",
                "refresh-token",
                DateTimeOffset.UtcNow.AddMinutes(20),
                [],
                Maybe<CharacterId>.Some(new CharacterId(12345)))));

        return tokenProvider;
    }
}