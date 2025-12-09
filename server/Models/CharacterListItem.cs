namespace server.Models;

public sealed record CharacterListItem(
    long CharacterId,
    string CharacterName,
    long CorporationId,
    string? CorporationName,
    bool IsDefault,
    bool HasValidToken);

public sealed record CharacterListResponse(List<CharacterListItem> Characters);
