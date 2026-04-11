namespace EVE.IPH.Domain.Assets.Models;

public sealed record AssetTreeNode(
    string NodeId,
    string? ParentNodeId,
    string DisplayText,
    AssetTreeNodeKind Kind,
    int FlagId,
    int FlagSort,
    bool Container,
    bool IsSingleton,
    long Quantity);