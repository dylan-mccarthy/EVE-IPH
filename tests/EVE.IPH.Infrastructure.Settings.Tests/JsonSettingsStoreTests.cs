using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Settings;
using EVE.IPH.Infrastructure.Settings.Models;

namespace EVE.IPH.Infrastructure.Settings.Tests;

public sealed class JsonSettingsStoreTests : IDisposable
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
    public async Task ReadAsync_WhenFileDoesNotExist_ReturnsMaybeNone()
    {
        JsonSettingsStore store = new(_tempDir);

        Maybe<ApplicationSettingsModel> result = await store.ReadAsync<ApplicationSettingsModel>();

        result.HasNoValue.Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_AndReadAsync_RoundTripsApplicationSettings()
    {
        JsonSettingsStore store = new(_tempDir);
        ApplicationSettingsModel original = new()
        {
            CheckForUpdatesOnStart = false,
            BaseSalesTaxRate = 3.5,
            ProxyAddress = "http://proxy.example.com",
            ProxyPort = 8080,
            SvrAveragePriceRegion = "Domain",
        };

        Result<bool> writeResult = await store.WriteAsync(original);
        Maybe<ApplicationSettingsModel> readResult = await store.ReadAsync<ApplicationSettingsModel>();

        writeResult.IsSuccess.Should().BeTrue();
        readResult.HasValue.Should().BeTrue();
        readResult.Value.CheckForUpdatesOnStart.Should().BeFalse();
        readResult.Value.BaseSalesTaxRate.Should().Be(3.5);
        readResult.Value.ProxyAddress.Should().Be("http://proxy.example.com");
        readResult.Value.ProxyPort.Should().Be(8080);
        readResult.Value.SvrAveragePriceRegion.Should().Be("Domain");
    }

    [Fact]
    public async Task WriteAsync_AndReadAsync_RoundTripsBpTabSettings()
    {
        JsonSettingsStore store = new(_tempDir);
        BpTabSettingsModel original = new()
        {
            BlueprintTypeSelection = "T2 Only",
            BrokerFeeRate = 0.03,
            ProductionLines = 5,
            HistoryRegion = "Sinq Laison",
        };

        await store.WriteAsync(original);
        Maybe<BpTabSettingsModel> readResult = await store.ReadAsync<BpTabSettingsModel>();

        readResult.HasValue.Should().BeTrue();
        readResult.Value.BlueprintTypeSelection.Should().Be("T2 Only");
        readResult.Value.BrokerFeeRate.Should().Be(0.03);
        readResult.Value.ProductionLines.Should().Be(5);
        readResult.Value.HistoryRegion.Should().Be("Sinq Laison");
    }

    [Fact]
    public async Task ReadAsync_WhenFileIsCorrupt_ReturnsMaybeNone()
    {
        Directory.CreateDirectory(_tempDir);
        string filePath = Path.Combine(_tempDir, "ApplicationSettingsModel.settings");
        await File.WriteAllTextAsync(filePath, "{ this is not valid JSON }}}");
        JsonSettingsStore store = new(_tempDir);

        Maybe<ApplicationSettingsModel> result = await store.ReadAsync<ApplicationSettingsModel>();

        result.HasNoValue.Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_CreatesDirectoryIfNotExists()
    {
        string nestedDir = Path.Combine(_tempDir, "nested", "deeply");
        JsonSettingsStore store = new(nestedDir);
        ApplicationSettingsModel model = new();

        Result<bool> result = await store.WriteAsync(model);

        result.IsSuccess.Should().BeTrue();
        Directory.Exists(nestedDir).Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_LeavesNoTempFileOnSuccess()
    {
        JsonSettingsStore store = new(_tempDir);
        ApplicationSettingsModel model = new();

        await store.WriteAsync(model);

        string[] tempFiles = Directory.GetFiles(_tempDir, "*.tmp");
        tempFiles.Should().BeEmpty();
    }

    [Fact]
    public async Task WriteAsync_AndReadAsync_RoundTripsShoppingListSettings()
    {
        JsonSettingsStore store = new(_tempDir);
        ShoppingListSettingsModel original = new()
        {
            AlwaysOnTop = true,
            Fees = true,
            CalcBuyBuyOrder = 2,
        };

        await store.WriteAsync(original);
        Maybe<ShoppingListSettingsModel> readResult = await store.ReadAsync<ShoppingListSettingsModel>();

        readResult.HasValue.Should().BeTrue();
        readResult.Value.AlwaysOnTop.Should().BeTrue();
        readResult.Value.Fees.Should().BeTrue();
        readResult.Value.CalcBuyBuyOrder.Should().Be(2);
    }
}
