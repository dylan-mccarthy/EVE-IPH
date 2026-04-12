using Avalonia;
using EVE.IPH.UI.Avalonia.Startup;
using Velopack;

namespace EVE.IPH.UI.Avalonia;

internal static class Program
{
    private static readonly Action<string> StartupStatusReporter = message =>
        Console.Error.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}] {message}");

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task Main(string[] args)
    {
        // Velopack must be initialized before anything else so that it can handle
        // first-run installation hooks and post-update cleanup.
        VelopackApp.Build().Run();

        // Ensure the user's app-data directory exists, create the SQLite database on
        // first run, and apply any pending schema migrations before the UI opens.
        StartupStatusReporter("Preparing startup prerequisites...");
        await StartupOrchestrator.PrepareAsync(StartupStatusReporter).ConfigureAwait(false);
        StartupStatusReporter("Startup prerequisites complete. Opening Avalonia window...");

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}