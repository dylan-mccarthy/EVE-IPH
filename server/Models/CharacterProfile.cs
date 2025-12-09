namespace server.Models;

public sealed record CharacterProfile(long CharacterId, string Name, int CorporationId, int? AllianceId, double? SecurityStatus, DateTime? Birthday);
