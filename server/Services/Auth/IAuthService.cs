using server.Models;

namespace server.Services.Auth;

public interface IAuthService
{
    Task<AuthStartResponse> StartAsync(CancellationToken ct = default);
    Task<AuthExchangeResponse> ExchangeAsync(AuthExchangeRequest request, CancellationToken ct = default);
}
