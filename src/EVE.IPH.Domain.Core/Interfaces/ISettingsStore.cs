using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Generic typed settings store. Infrastructure provides a JSON-backed
/// implementation; consumers depend only on this interface.
/// </summary>
public interface ISettingsStore
{
    /// <summary>
    /// Reads the settings of type <typeparamref name="T"/>.
    /// Returns <see cref="Maybe{T}.None"/> if no settings file exists yet for this type.
    /// </summary>
    Task<Maybe<T>> ReadAsync<T>(CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Writes the settings of type <typeparamref name="T"/> to persistent storage.
    /// The write must be atomic — a crash during writing must leave the previous
    /// settings intact.
    /// </summary>
    Task<Result<bool>> WriteAsync<T>(T settings, CancellationToken cancellationToken = default) where T : class;
}
