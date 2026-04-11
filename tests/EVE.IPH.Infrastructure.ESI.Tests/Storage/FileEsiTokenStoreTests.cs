using System.Text;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Storage;

namespace EVE.IPH.Infrastructure.ESI.Tests.Storage;

public sealed class FileEsiTokenStoreTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ReadAsync_WhenFileDoesNotExist_ReturnsNone()
    {
        FileEsiTokenStore store = CreateStore();

        Maybe<EsiTokenRecord> result = await store.ReadAsync();

        result.HasNoValue.Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_AndReadAsync_RoundTripsToken()
    {
        FileEsiTokenStore store = CreateStore();
        EsiTokenRecord token = CreateTokenRecord();

        Result<EsiTokenRecord> writeResult = await store.WriteAsync(token);
        Maybe<EsiTokenRecord> readResult = await store.ReadAsync();

        writeResult.IsSuccess.Should().BeTrue();
        readResult.HasValue.Should().BeTrue();
        readResult.Value.Should().BeEquivalentTo(token);
    }

    [Fact]
    public async Task ClearAsync_RemovesStoredTokenFile()
    {
        string filePath = Path.Combine(_tempDir, "esi-token.json");
        FileEsiTokenStore store = new(filePath);

        await store.WriteAsync(CreateTokenRecord());
        Result<bool> clearResult = await store.ClearAsync();

        clearResult.IsSuccess.Should().BeTrue();
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task WriteAsync_OnWindows_DoesNotPersistPlaintextToken()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string filePath = Path.Combine(_tempDir, "esi-token.json");
        FileEsiTokenStore store = new(filePath);

        await store.WriteAsync(CreateTokenRecord(accessToken: "plain-access-token"));
        string fileContents = await File.ReadAllTextAsync(filePath, Encoding.UTF8);

        fileContents.Should().NotContain("plain-access-token");
    }

    private FileEsiTokenStore CreateStore() => new(Path.Combine(_tempDir, "esi-token.json"));

    private static EsiTokenRecord CreateTokenRecord(string accessToken = "access-token") => new(
        accessToken,
        "refresh-token",
        DateTimeOffset.UtcNow.AddMinutes(20),
        ["esi-skills.read_skills", "esi-characters.read_standings"],
        Maybe<CharacterId>.Some(new CharacterId(123456)));
}