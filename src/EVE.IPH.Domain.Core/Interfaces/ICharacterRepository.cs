using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Persists and retrieves EVE character records from the application database.
/// </summary>
public interface ICharacterRepository
{
    /// <summary>Returns all stored characters.</summary>
    Task<Result<IReadOnlyList<CharacterRecord>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a single character by ID, or <see cref="Maybe{T}.None"/> if not found.</summary>
    Task<Maybe<CharacterRecord>> GetByIdAsync(CharacterId characterId, CancellationToken cancellationToken = default);

    /// <summary>Inserts or updates the character record.</summary>
    Task<Result<CharacterRecord>> UpsertAsync(CharacterRecord character, CancellationToken cancellationToken = default);

    /// <summary>Removes the character and all associated data.</summary>
    Task<Result<bool>> DeleteAsync(CharacterId characterId, CancellationToken cancellationToken = default);
}

/// <summary>A stored EVE character.</summary>
/// <param name="CharacterId">The EVE character ID.</param>
/// <param name="Name">The character name.</param>
/// <param name="CorporationId">The character's current corporation.</param>
/// <param name="AllianceId">The character's current alliance, if any.</param>
/// <param name="IsDefault">Whether this is the default character used on application start.</param>
public sealed record CharacterRecord(
    CharacterId CharacterId,
    string Name,
    CorporationId CorporationId,
    Maybe<AllianceId> AllianceId,
    bool IsDefault);
