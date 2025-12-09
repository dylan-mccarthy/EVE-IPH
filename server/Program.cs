using Microsoft.Extensions.Options;
using server.Endpoints;
using server.Endpoints.Auth;
using server.Endpoints.Characters;
using server.Endpoints.Blueprints;
using server.Endpoints.Manufacturing;
using server.Endpoints.Market;
using server.Endpoints.Settings;
using server.Infrastructure;
using server.Services.Blueprints;
using server.Services.Auth;
using server.Services.Characters;
using server.Services.Manufacturing;
using server.Services.Market;
using server.Services.Settings;

var builder = WebApplication.CreateBuilder(args);

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

// Domain services (stubs for now)
builder.Services.AddScoped<IBlueprintService, BlueprintService>();
builder.Services.AddScoped<IManufacturingService, ManufacturingService>();
builder.Services.AddScoped<IMarketService, MarketService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddSingleton<IAuthService, AuthService>();

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

app.Run();
