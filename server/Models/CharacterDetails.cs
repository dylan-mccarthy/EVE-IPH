namespace server.Models;

public sealed record CharacterDetails(
    long CharacterId,
    string CharacterName,
    string Gender,
    string Birthday,
    int RaceId,
    int BloodlineId,
    int AncestryId,
    string? Description,
    double? SecurityStatus,
    CorporationInfo Corporation,
    WalletInfo? Wallet,
    SkillsSummary Skills,
    List<string> Scopes);

public sealed record CorporationInfo(
    long CorporationId,
    string CorporationName,
    string? Ticker,
    int? MemberCount,
    long? AllianceId,
    string? AllianceName);

public sealed record WalletInfo(
    double Balance);

public sealed record SkillsSummary(
    long TotalSp,
    int TotalSkills,
    int UnallocatedSp);
