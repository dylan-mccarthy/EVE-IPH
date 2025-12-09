using server.Endpoints;
using server.Endpoints.Blueprints;
using server.Endpoints.Manufacturing;
using server.Endpoints.Market;
using server.Endpoints.Settings;
using server.Infrastructure;
using server.Services.Blueprints;
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
builder.Services.AddSingleton<ISqliteConnectionFactory, SqliteConnectionFactory>();

// Domain services (stubs for now)
builder.Services.AddScoped<IBlueprintService, BlueprintService>();
builder.Services.AddScoped<IManufacturingService, ManufacturingService>();
builder.Services.AddScoped<IMarketService, MarketService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("cors");

app.MapHealthEndpoints();
app.MapBlueprintEndpoints();
app.MapManufacturingEndpoints();
app.MapMarketEndpoints();
app.MapSettingsEndpoints();

app.Run();
