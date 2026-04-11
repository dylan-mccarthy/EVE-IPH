using EVE.IPH.Infrastructure.ESI.Authentication;
using EVE.IPH.Infrastructure.ESI.Interfaces;
using EVE.IPH.Infrastructure.ESI.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace EVE.IPH.Infrastructure.ESI.DependencyInjection;

/// <summary>
/// Registers the typed ESI client and its supporting handlers.
/// </summary>
public static class EsiServiceCollectionExtensions
{
    private static readonly Uri DefaultBaseAddress = new("https://esi.evetech.net/latest/");
    private static readonly Uri DefaultSsoBaseAddress = new("https://login.eveonline.com/");

    public static IServiceCollection AddEsiInfrastructure(
        this IServiceCollection services,
        Uri? baseAddress = null,
        Uri? ssoBaseAddress = null,
        EsiSsoOptions? ssoOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(ssoOptions ?? EsiSsoOptions.Default);
        services.AddSingleton<Domain.Core.Interfaces.IEsiTokenStore, FileEsiTokenStore>();
        services.AddSingleton<IEsiCallbackListener, TcpEsiCallbackListener>();
        services.AddSingleton<IEsiBrowserLauncher, DefaultBrowserLauncher>();
        services.AddTransient<BearerTokenHandler>();
        services.AddTransient<EsiResilienceHandler>();

        services.AddHttpClient<IEsiSsoClient, EsiSsoClient>(client => client.BaseAddress = ssoBaseAddress ?? DefaultSsoBaseAddress);
        services.AddTransient<IEsiTokenProvider, EsiTokenProvider>();
        services.AddTransient<IEsiInteractiveLoginService, EsiInteractiveLoginService>();

        services
            .AddHttpClient<IEsiClient, EsiClient>(client => client.BaseAddress = baseAddress ?? DefaultBaseAddress)
            .AddHttpMessageHandler<BearerTokenHandler>()
            .AddHttpMessageHandler<EsiResilienceHandler>();

        return services;
    }
}