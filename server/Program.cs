using Microsoft.Extensions.Options;
using server.Endpoints;
using server.Endpoints.Auth;
using server.Endpoints.Characters;
using server.Endpoints.Blueprints;
using server.Endpoints.Manufacturing;
using server.Endpoints.Market;
using server.Endpoints.Settings;
using server.Endpoints.Wallet;
using server.Endpoints.Assets;
using server.Endpoints.Industry;
using server.Infrastructure;
using server.Services.Blueprints;
using server.Services.Auth;
using server.Services.Characters;
using server.Services.Manufacturing;
using server.Services.Market;
using server.Services.Settings;
using server.Services.Wallet;
using server.Services.Assets;
using server.Services.Industry;

// Load environment variables from .env file
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Override configuration with environment variables
var clientId = Environment.GetEnvironmentVariable("EVE_SSO_CLIENT_ID");
var clientSecret = Environment.GetEnvironmentVariable("EVE_SSO_CLIENT_SECRET");

if (!string.IsNullOrEmpty(clientId))
{
    builder.Configuration["EveSso:ClientId"] = clientId;
}

if (!string.IsNullOrEmpty(clientSecret))
{
    builder.Configuration["EveSso:ClientSecret"] = clientSecret;
}

// Services
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("cors", policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddSingleton(AppInfo.Create(builder.Configuration, builder.Environment));
builder.Services.Configure<SqliteOptions>(builder.Configuration.GetSection("Data"));
builder.Services.Configure<EveSsoOptions>(builder.Configuration.GetSection("EveSso"));
builder.Services.Configure<EsiOptions>(builder.Configuration.GetSection("Esi"));
builder.Services.AddSingleton<ISqliteConnectionFactory, SqliteConnectionFactory>();
builder.Services.AddHttpClient("sso", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<EveSsoOptions>>().Value;
    client.BaseAddress = new Uri(options.Authority.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddHttpClient<ICharacterService, CharacterService>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<EsiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddHttpClient<ITokenRefreshService, TokenRefreshService>((sp, client) =>
{
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddHttpClient<IWalletService, WalletService>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<EsiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddHttpClient<IAssetsService, AssetsService>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<EsiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddHttpClient<IIndustryService, IndustryService>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<EsiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

// Domain services (stubs for now)
builder.Services.AddScoped<IBlueprintService, BlueprintService>();
builder.Services.AddScoped<IManufacturingService, ManufacturingService>();
builder.Services.AddScoped<IMarketService, MarketService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<ITokenStore, TokenStore>();
builder.Services.AddSingleton<ITokenRefreshService, TokenRefreshService>();
builder.Services.AddSingleton<ICharacterPersistenceService, CharacterPersistenceService>();

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("cors");

app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapBlueprintEndpoints();
app.MapManufacturingEndpoints();
app.MapMarketEndpoints();
app.MapSettingsEndpoints();
app.MapCharacterEndpoints();
app.MapWalletEndpoints();
app.MapAssetsEndpoints();
app.MapIndustryEndpoints();

app.Run();
