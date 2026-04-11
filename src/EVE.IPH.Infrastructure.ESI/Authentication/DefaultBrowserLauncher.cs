using System.Diagnostics;
using EVE.IPH.Infrastructure.ESI.Interfaces;

namespace EVE.IPH.Infrastructure.ESI.Authentication;

/// <summary>
/// Opens the system browser using the platform shell.
/// </summary>
public sealed class DefaultBrowserLauncher : IEsiBrowserLauncher
{
    public Task OpenAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);
        cancellationToken.ThrowIfCancellationRequested();

        ProcessStartInfo startInfo = new()
        {
            FileName = uri.AbsoluteUri,
            UseShellExecute = true,
        };

        Process? process = Process.Start(startInfo);
        if (process is null)
        {
            throw new InvalidOperationException("The system browser could not be started.");
        }

        return Task.CompletedTask;
    }
}