using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.Domain.Assets.Models;

public sealed record AssetTypeMetadata(
    TypeId TypeId,
    string TypeName,
    string GroupName,
    string CategoryName);