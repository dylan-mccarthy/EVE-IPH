namespace server.Models;

public sealed record CharacterSyncResponse(
    bool Success,
    string Message,
    CharacterSyncStats Stats,
    List<string>? Errors = null
);

public sealed record CharacterSyncStats(
    int SkillsSynced,
    int AssetsSynced,
    int IndustryJobsSynced,
    int BlueprintsSynced,
    int WalletTransactionsSynced,
    int MarketOrdersSynced
);
