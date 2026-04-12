using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Reads item type information from the EVE Static Data Export (SDE).
/// </summary>
public interface IItemRepository
{
    /// <summary>Returns the item record for the given type ID, or <see cref="Maybe{T}.None"/> if not found.</summary>
    Task<Maybe<ItemRecord>> GetItemAsync(TypeId typeId, CancellationToken cancellationToken = default);

    /// <summary>Returns the display name for the given type ID, or <see cref="Maybe{T}.None"/> if not found.</summary>
    Task<Maybe<string>> GetItemNameAsync(TypeId typeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all items in a given market group or inventory group, suitable for populating
    /// list views and price lookups.
    /// </summary>
    Task<Result<IReadOnlyList<ItemRecord>>> GetItemsByGroupAsync(int groupId, CancellationToken cancellationToken = default);

    /// <summary>Returns all items whose group name matches any of the supplied names.</summary>
    Task<Result<IReadOnlyList<ItemRecord>>> GetItemsByGroupNamesAsync(IReadOnlyCollection<string> groupNames, CancellationToken cancellationToken = default);

    /// <summary>Returns all items whose category name starts with the supplied prefix.</summary>
    Task<Result<IReadOnlyList<ItemRecord>>> GetItemsByCategoryPrefixAsync(string categoryNamePrefix, CancellationToken cancellationToken = default);
}

/// <summary>Core attributes of an inventory type from the SDE.</summary>
/// <param name="TypeId">The type ID.</param>
/// <param name="TypeName">The display name.</param>
/// <param name="GroupId">The inventory group ID.</param>
/// <param name="GroupName">The inventory group name.</param>
/// <param name="CategoryId">The inventory category ID.</param>
/// <param name="Volume">The volume per unit in m³.</param>
/// <param name="PortionSize">The portion size (items produced per manufacturing run).</param>
public sealed record ItemRecord(
    TypeId TypeId,
    string TypeName,
    int GroupId,
    string GroupName,
    int CategoryId,
    double Volume,
    int PortionSize);
