namespace server.Models;

public sealed record AuthExchangeResponse(long CharacterId, string CharacterName, string AccessToken, DateTimeOffset ExpiresAtUtc, string RefreshToken, CharacterProfile Profile);
