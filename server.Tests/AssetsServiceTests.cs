using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using server.Services.Assets;
using server.Services.Auth;
using Xunit;

namespace server.Tests.Services;

public class AssetsServiceTests_New
{
    private readonly Mock<ITokenStore> _mockTokenStore;
    private readonly Mock<ILogger<AssetsService>> _mockLogger;

    public AssetsServiceTests_New()
    {
        _mockTokenStore = new Mock<ITokenStore>();
        _mockLogger = new Mock<ILogger<AssetsService>>();
    }

    [Fact]
    public async Task GetAssetsAsync_WithValidToken_ReturnsAssets()
    {
        // Arrange
        var characterId = 12345L;
        var accessToken = "valid_token";
        
        _mockTokenStore
            .Setup(x => x.GetTokenAsync(characterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredToken(characterId, accessToken, DateTimeOffset.UtcNow.AddHours(1), "refresh", "scopes"));

        var handler = new StubHandler(_ =>
        {
            const string json = @"[
                {
                    ""item_id"": 1000000000001,
                    ""location_id"": 60003760,
                    ""location_flag"": ""Hangar"",
                    ""location_type"": ""station"",
                    ""type_id"": 34,
                    ""quantity"": 100,
                    ""is_singleton"": false
                },
                {
                    ""item_id"": 1000000000002,
                    ""location_id"": 60003760,
                    ""location_flag"": ""Hangar"",
                    ""location_type"": ""station"",
                    ""type_id"": 11537,
                    ""quantity"": 1,
                    ""is_singleton"": true,
                    ""is_blueprint_copy"": true
                }
            ]";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://esi.test/") };
        var service = new AssetsService(
            client,
            _mockTokenStore.Object,
            _mockLogger.Object);

        // Act
        var assets = await service.GetAssetsAsync(characterId);

        // Assert
        assets.Should().NotBeEmpty();
        assets.Should().HaveCount(2);
        
        var firstAsset = assets[0];
        firstAsset.ItemId.Should().Be(1000000000001);
        firstAsset.TypeId.Should().Be(34);
        firstAsset.Quantity.Should().Be(100);
        firstAsset.IsSingleton.Should().BeFalse();
        
        var secondAsset = assets[1];
        secondAsset.IsSingleton.Should().BeTrue();
        secondAsset.IsBlueprintCopy.Should().BeTrue();
    }

    [Fact]
    public async Task GetAssetsAsync_WhenNoToken_ThrowsInvalidOperation()
    {
        // Arrange
        var characterId = 12345L;
        
        _mockTokenStore
            .Setup(x => x.GetTokenAsync(characterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoredToken?)null);

        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://esi.test/") };
        var service = new AssetsService(
            client,
            _mockTokenStore.Object,
            _mockLogger.Object);

        // Act
        var act = async () => await service.GetAssetsAsync(characterId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetAssetsAsync_WithEmptyResponse_ReturnsEmpty()
    {
        // Arrange
        var characterId = 12345L;
        var accessToken = "valid_token";
        
        _mockTokenStore
            .Setup(x => x.GetTokenAsync(characterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredToken(characterId, accessToken, DateTimeOffset.UtcNow.AddHours(1), "refresh", "scopes"));

        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json")
        });
        
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://esi.test/") };
        var service = new AssetsService(
            client,
            _mockTokenStore.Object,
            _mockLogger.Object);

        // Act
        var assets = await service.GetAssetsAsync(characterId);

        // Assert
        assets.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAssetsAsync_WhenEsiError_ReturnsEmpty()
    {
        // Arrange
        var characterId = 12345L;
        var accessToken = "valid_token";
        
        _mockTokenStore
            .Setup(x => x.GetTokenAsync(characterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredToken(characterId, accessToken, DateTimeOffset.UtcNow.AddHours(1), "refresh", "scopes"));

        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("{\"error\": \"server error\"}", Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://esi.test/") };
        var service = new AssetsService(
            client,
            _mockTokenStore.Object,
            _mockLogger.Object);

        // Act
        var assets = await service.GetAssetsAsync(characterId);

        // Assert
        assets.Should().BeEmpty();
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(handler(request));
        }
    }
}
