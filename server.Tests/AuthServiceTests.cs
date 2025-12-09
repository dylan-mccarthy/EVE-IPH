using System;
using System.Net.Http;
using FluentAssertions;
using Microsoft.Extensions.Options;
using server.Infrastructure;
using server.Models;
using server.Services.Auth;
using server.Services.Characters;
using Xunit;

namespace server.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task StartAsync_ReturnsStateAndUrl()
    {
        var service = CreateService();
        var result = await service.StartAsync();

        result.State.Should().NotBeNullOrEmpty();
        result.Url.Should().Contain("response_type=code");
        result.Url.Should().Contain(result.State);
        result.Url.Should().Contain("client_id=test-client");
        result.Url.Should().Contain("code_challenge=");
        result.Url.Should().Contain("code_challenge_method=S256");
    }

    [Fact]
    public async Task StartAsync_UsesLegacyClientIdWhenMissing()
    {
        var service = CreateService(new EveSsoOptions
        {
            Authority = "https://login.eveonline.com",
            ClientId = string.Empty,
            ClientSecret = string.Empty,
            RedirectUri = "https://localhost/callback",
            Scopes = "esi-skills.read_skills.v1"
        });

        var result = await service.StartAsync();

        result.Url.Should().Contain($"client_id={Uri.EscapeDataString(EveSsoOptions.LegacyClientId)}");
    }

    private static AuthService CreateService(EveSsoOptions? optionsOverride = null)
    {
        var options = Options.Create(optionsOverride ?? new EveSsoOptions
        {
            Authority = "https://login.eveonline.com",
            ClientId = "test-client",
            ClientSecret = "test-secret",
            RedirectUri = "https://localhost/callback",
            Scopes = "esi-skills.read_skills.v1"
        });

        var httpFactory = new StubHttpClientFactory();
        var characters = new StubCharacterService();
        return new AuthService(options, httpFactory, characters);
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient(new HttpClientHandler());
        }
    }

    private sealed class StubCharacterService : ICharacterService
    {
        public Task<CharacterProfile> GetProfileAsync(long characterId, string accessToken, CancellationToken ct = default)
        {
            return Task.FromResult(new CharacterProfile(characterId, "stub", 0, null, null, null));
        }
    }
}
