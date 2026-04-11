using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Reads blueprint definitions from the EVE Static Data Export (SDE).
/// Infrastructure provides a SQLite-backed implementation; domain code
/// depends only on this contract.
/// </summary>
public interface IBlueprintRepository
{
    /// <summary>Returns the blueprint record for the given blueprint type ID, or <see cref="Maybe{T}.None"/> if not found.</summary>
    Task<Maybe<BlueprintRecord>> GetBlueprintAsync(BlueprintId blueprintId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all materials required for a specific blueprint activity
    /// (e.g. manufacturing inputs for one run at 0 ME).
    /// </summary>
    Task<Result<IReadOnlyList<BlueprintMaterial>>> GetMaterialsAsync(
        BlueprintId blueprintId,
        ActivityType activity,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the required skills for a blueprint activity.</summary>
    Task<Result<IReadOnlyList<SkillRequirement>>> GetRequiredSkillsAsync(
        BlueprintId blueprintId,
        ActivityType activity,
        CancellationToken cancellationToken = default);
}

/// <summary>Core attributes of a blueprint type from the SDE.</summary>
/// <param name="BlueprintId">The blueprint type ID.</param>
/// <param name="ProductTypeId">The type ID of the item produced.</param>
/// <param name="ProductName">The name of the produced item.</param>
/// <param name="TechLevel">Technology level of the produced item.</param>
/// <param name="MaxProductionLimit">Maximum number of runs per copy.</param>
/// <param name="ManufacturingTime">Base manufacturing time in seconds for one run at 0 TE.</param>
/// <param name="ResearchMeTime">Base ME research time in seconds per level.</param>
/// <param name="ResearchTeTime">Base TE research time in seconds per level.</param>
/// <param name="CopyTime">Base copy time in seconds per run.</param>
/// <param name="InventionTime">Base invention time in seconds per attempt.</param>
public sealed record BlueprintRecord(
    BlueprintId BlueprintId,
    TypeId ProductTypeId,
    string ProductName,
    TechLevel TechLevel,
    int MaxProductionLimit,
    long ManufacturingTime,
    long ResearchMeTime,
    long ResearchTeTime,
    long CopyTime,
    long InventionTime);

/// <summary>A single material line in a blueprint activity.</summary>
/// <param name="TypeId">The type ID of the required material.</param>
/// <param name="TypeName">The display name of the material.</param>
/// <param name="Quantity">The base quantity required (before ME reduction).</param>
public sealed record BlueprintMaterial(
    TypeId TypeId,
    string TypeName,
    long Quantity);

/// <summary>A skill required to perform a blueprint activity.</summary>
/// <param name="SkillTypeId">The type ID of the required skill.</param>
/// <param name="Level">The minimum level required.</param>
public sealed record SkillRequirement(TypeId SkillTypeId, int Level);
