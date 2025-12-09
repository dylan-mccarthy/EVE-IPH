namespace server.Models;

public sealed record ApiError(string Message, string? Detail = null);
