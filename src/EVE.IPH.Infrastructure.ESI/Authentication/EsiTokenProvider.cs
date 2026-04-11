using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Interfaces;

namespace EVE.IPH.Infrastructure.ESI.Authentication;

/// <summary>
/// Reads the currently stored access token and refreshes it as needed.
/// </summary>
public sealed class EsiTokenProvider(IEsiTokenStore tokenStore, IEsiSsoClient ssoClient, TimeProvider timeProvider) : IEsiTokenProvider
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(1);

    private readonly IEsiTokenStore _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
    private readonly IEsiSsoClient _ssoClient = ssoClient ?? throw new ArgumentNullException(nameof(ssoClient));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<Result<EsiAccessToken>> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        Maybe<EsiTokenRecord> tokenRecord = await _tokenStore.ReadAsync(cancellationToken).ConfigureAwait(false);
        return await GetAccessTokenAsync(tokenRecord, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<EsiAccessToken>> GetAccessTokenAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        Maybe<EsiTokenRecord> tokenRecord = await _tokenStore.ReadAsync(characterId, cancellationToken).ConfigureAwait(false);
        return await GetAccessTokenAsync(tokenRecord, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<EsiAccessToken>> RefreshAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        Maybe<EsiTokenRecord> tokenRecord = await _tokenStore.ReadAsync(cancellationToken).ConfigureAwait(false);
        return await RefreshAccessTokenAsync(tokenRecord, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<EsiAccessToken>> RefreshAccessTokenAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        Maybe<EsiTokenRecord> tokenRecord = await _tokenStore.ReadAsync(characterId, cancellationToken).ConfigureAwait(false);
        return await RefreshAccessTokenAsync(tokenRecord, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<EsiAccessToken>> GetAccessTokenAsync(Maybe<EsiTokenRecord> tokenRecord, CancellationToken cancellationToken)
    {
        if (tokenRecord.HasNoValue)
        {
            return Result<EsiAccessToken>.Failure("ESI_TOKEN_NOT_FOUND", "No ESI token is stored for the requested character.");
        }

        EsiAccessToken token = EsiAccessToken.FromRecord(tokenRecord.Value);
        if (token.ExpiresAtUtc - _timeProvider.GetUtcNow() > RefreshSkew)
        {
            return Result<EsiAccessToken>.Success(token);
        }

        return await RefreshAccessTokenAsync(tokenRecord, cancellationToken).ConfigureAwait(false);
    }

    private Task<Result<EsiAccessToken>> RefreshAccessTokenAsync(Maybe<EsiTokenRecord> tokenRecord, CancellationToken cancellationToken)
    {
        if (tokenRecord.HasNoValue)
        {
            return Task.FromResult(Result<EsiAccessToken>.Failure("ESI_REFRESH_NOT_POSSIBLE", "No refresh token is available for the requested character."));
        }

        return _ssoClient.RefreshAccessTokenAsync(tokenRecord.Value.RefreshToken, cancellationToken);
    }
}