namespace EVE.IPH.Infrastructure.ESI.Interfaces;

/// <summary>
/// Opens a browser to a supplied authorization URL.
/// </summary>
public interface IEsiBrowserLauncher
{
    Task OpenAsync(Uri uri, CancellationToken cancellationToken = default);
}