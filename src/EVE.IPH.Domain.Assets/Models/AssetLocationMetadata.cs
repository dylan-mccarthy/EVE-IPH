namespace EVE.IPH.Domain.Assets.Models;

public sealed record AssetLocationMetadata(
    string LocationName,
    string FlagText,
    bool Container,
    int FlagSort);