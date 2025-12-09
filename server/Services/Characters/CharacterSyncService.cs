using Microsoft.Extensions.Logging;
using server.Models;
using server.Services.Assets;
using server.Services.Auth;
using server.Services.Blueprints;
using server.Services.Industry;
using server.Services.Market;
using server.Services.Wallet;

namespace server.Services.Characters;

public sealed class CharacterSyncService : ICharacterSyncService
{
    private readonly ICharacterService _characterService;
    private readonly IAssetsService _assetsService;
    private readonly IIndustryService _industryService;
    private readonly IWalletService _walletService;
    private readonly IMarketOrdersService _marketOrdersService;
    private readonly ICharacterPersistenceService _persistenceService;
    private readonly ITokenRefreshService _tokenRefresh;
    private readonly ILogger<CharacterSyncService> _logger;

    public CharacterSyncService(
        ICharacterService characterService,
        IAssetsService assetsService,
        IIndustryService industryService,
        IWalletService walletService,
        IMarketOrdersService marketOrdersService,
        ICharacterPersistenceService persistenceService,
        ITokenRefreshService tokenRefresh,
        ILogger<CharacterSyncService> logger)
    {
        _characterService = characterService;
        _assetsService = assetsService;
        _industryService = industryService;
        _walletService = walletService;
        _marketOrdersService = marketOrdersService;
        _persistenceService = persistenceService;
        _tokenRefresh = tokenRefresh;
        _logger = logger;
    }

    public async Task<CharacterSyncResponse> SyncCharacterDataAsync(long characterId, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting sync for character {CharacterId}", characterId);
        var errors = new List<string>();
        var stats = new CharacterSyncStats(0, 0, 0, 0, 0, 0);

        // Verify token is valid
        var tokenResult = await _tokenRefresh.RefreshTokenIfNeededAsync(characterId, ct);
        if (!tokenResult.Success || tokenResult.AccessToken is null)
        {
            return new CharacterSyncResponse(
                false,
                "Token refresh failed",
                stats,
                new List<string> { "Unable to authenticate with ESI" }
            );
        }

        // Sync skills
        try
        {
            _logger.LogInformation("Syncing skills for character {CharacterId}", characterId);
            var skills = await _characterService.GetSkillsAsync(characterId, tokenResult.AccessToken, ct);
            // Save skills to database via persistence service
            await _persistenceService.SaveSkillsAsync(characterId, skills, ct);
            stats = stats with { SkillsSynced = skills.SkillGroups.Sum(g => g.Skills.Count) };
            _logger.LogInformation("Synced {Count} skills for character {CharacterId}", stats.SkillsSynced, characterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync skills for character {CharacterId}", characterId);
            errors.Add($"Skills sync failed: {ex.Message}");
        }

        // Sync assets
        try
        {
            _logger.LogInformation("Syncing assets for character {CharacterId}", characterId);
            var assets = await _assetsService.GetAssetsAsync(characterId, ct);
            stats = stats with { AssetsSynced = assets.Count };
            _logger.LogInformation("Synced {Count} assets for character {CharacterId}", stats.AssetsSynced, characterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync assets for character {CharacterId}", characterId);
            errors.Add($"Assets sync failed: {ex.Message}");
        }

        // Sync industry jobs
        try
        {
            _logger.LogInformation("Syncing industry jobs for character {CharacterId}", characterId);
            var jobs = await _industryService.GetIndustryJobsAsync(characterId, true, ct);
            stats = stats with { IndustryJobsSynced = jobs.Count };
            _logger.LogInformation("Synced {Count} industry jobs for character {CharacterId}", stats.IndustryJobsSynced, characterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync industry jobs for character {CharacterId}", characterId);
            errors.Add($"Industry jobs sync failed: {ex.Message}");
        }

        // Sync wallet transactions
        try
        {
            _logger.LogInformation("Syncing wallet transactions for character {CharacterId}", characterId);
            var transactions = await _walletService.GetWalletTransactionsAsync(characterId, ct);
            stats = stats with { WalletTransactionsSynced = transactions.Count };
            _logger.LogInformation("Synced {Count} wallet transactions for character {CharacterId}", stats.WalletTransactionsSynced, characterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync wallet transactions for character {CharacterId}", characterId);
            errors.Add($"Wallet transactions sync failed: {ex.Message}");
        }

        // Sync market orders
        try
        {
            _logger.LogInformation("Syncing market orders for character {CharacterId}", characterId);
            var orders = await _marketOrdersService.GetMarketOrdersAsync(characterId, ct);
            stats = stats with { MarketOrdersSynced = orders.Count };
            _logger.LogInformation("Synced {Count} market orders for character {CharacterId}", stats.MarketOrdersSynced, characterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync market orders for character {CharacterId}", characterId);
            errors.Add($"Market orders sync failed: {ex.Message}");
        }

        var success = errors.Count == 0;
        var message = success
            ? $"Successfully synced all data for character {characterId}"
            : $"Sync completed with {errors.Count} error(s)";

        _logger.LogInformation("Sync completed for character {CharacterId}: {Message}", characterId, message);

        return new CharacterSyncResponse(success, message, stats, errors.Count > 0 ? errors : null);
    }
}
