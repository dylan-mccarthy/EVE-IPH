namespace server.Services.Wallet;

public interface IWalletService
{
    Task<decimal> GetWalletBalanceAsync(long characterId, CancellationToken ct = default);
    Task<List<WalletTransaction>> GetWalletTransactionsAsync(long characterId, CancellationToken ct = default);
    Task<List<WalletJournalEntry>> GetWalletJournalAsync(long characterId, CancellationToken ct = default);
}

public sealed record WalletTransaction(
    long TransactionId,
    DateTimeOffset Date,
    long TypeId,
    long LocationId,
    int Quantity,
    decimal UnitPrice,
    long ClientId,
    bool IsBuy,
    bool IsPersonal,
    long JournalRefId
);

public sealed record WalletJournalEntry(
    long Id,
    DateTimeOffset Date,
    string RefType,
    long? FirstPartyId,
    long? SecondPartyId,
    decimal? Amount,
    decimal? Balance,
    string Description,
    string Reason,
    long? TaxReceiverId,
    decimal? Tax
);
