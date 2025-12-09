namespace server.Models;

public sealed record AuthExchangeRequest(string Code, string State);
