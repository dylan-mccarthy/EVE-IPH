using EVE.IPH.Infrastructure.ESI.Authentication;
using EVE.IPH.Infrastructure.ESI.Interfaces;
using EVE.IPH.Infrastructure.ESI.Market;
using EVE.IPH.Infrastructure.ESI.Storage;
using Microsoft.Extensions.DependencyInjection;
using EVE.IPH.Domain.Core.Interfaces;

namespace EVE.IPH.Infrastructure.ESI.DependencyInjection;

/// <summary>
/// Registers the typed ESI client and its supporting handlers.
/// </summary>
public static class EsiServiceCollectionExtensions
{
    private static readonly Uri DefaultBaseAddress = new("https://esi.evetech.net/latest/");
    private static readonly Uri DefaultSsoBaseAddress = new("https://login.eveonline.com/");
    private static readonly Uri DefaultEveMarketerBaseAddress = new("https://api.evemarketer.com/");
    private static readonly Uri DefaultFuzzworksBaseAddress = new("https://market.fuzzwork.co.uk/");

    public static IServiceCollection AddEsiInfrastructure(
        this IServiceCollection services,
        Uri? baseAddress = null,
        Uri? ssoBaseAddress = null,
        EsiSsoOptions? ssoOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(ssoOptions ?? EsiSsoOptions.Default);
        services.AddSingleton<IEsiTokenStore, FileEsiTokenStore>();
        services.AddSingleton<IEsiCallbackListener, TcpEsiCallbackListener>();
        services.AddSingleton<IEsiBrowserLauncher, DefaultBrowserLauncher>();
        services.AddTransient<BearerTokenHandler>();
        services.AddTransient<EsiResilienceHandler>();

        services.AddHttpClient<IEsiSsoClient, EsiSsoClient>(client => client.BaseAddress = ssoBaseAddress ?? DefaultSsoBaseAddress);
        services.AddTransient<IEsiTokenProvider, EsiTokenProvider>();
        services.AddTransient<IEsiInteractiveLoginService, EsiInteractiveLoginService>();
        services.AddTransient<ICharacterDataSource, EsiCharacterDataSource>();
        services.AddTransient<ICharacterResearchAgentDataSource, EsiCharacterResearchAgentDataSource>();
        services.AddTransient<IMarketPriceSourceResolver, MarketPriceSourceResolver>();

        services
            .AddHttpClient<TranquilityMarketPriceSource>(client => client.BaseAddress = baseAddress ?? DefaultBaseAddress)
            .AddHttpMessageHandler<EsiResilienceHandler>();
        services.AddTransient<IMarketPriceSource>(serviceProvider => serviceProvider.GetRequiredService<TranquilityMarketPriceSource>());

        services.AddHttpClient<EveMarketerMarketPriceSource>(client => client.BaseAddress = DefaultEveMarketerBaseAddress);
        services.AddTransient<IMarketPriceSource>(serviceProvider => serviceProvider.GetRequiredService<EveMarketerMarketPriceSource>());

        services.AddHttpClient<FuzzworksMarketPriceSource>(client => client.BaseAddress = DefaultFuzzworksBaseAddress);
        services.AddTransient<IMarketPriceSource>(serviceProvider => serviceProvider.GetRequiredService<FuzzworksMarketPriceSource>());

        services
            .AddHttpClient<IEsiClient, EsiClient>(client => client.BaseAddress = baseAddress ?? DefaultBaseAddress)
            .AddHttpMessageHandler<BearerTokenHandler>()
            .AddHttpMessageHandler<EsiResilienceHandler>();

        return services;
    }
}