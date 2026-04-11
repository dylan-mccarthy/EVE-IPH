using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class ApplicationRestartService : IApplicationRestartService
{
    public void Restart()
    {
        string? processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath))
        {
            throw new InvalidOperationException("Unable to determine the current application path for restart.");
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = processPath,
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(processPath) ?? AppContext.BaseDirectory,
        });

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
            return;
        }

        Environment.Exit(0);
    }
}