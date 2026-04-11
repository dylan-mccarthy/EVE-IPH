namespace EVE.IPH.Infrastructure.Settings.Models;

public sealed record StaticDataSettingsModel
{
    public const long DefaultSupportedBuildNumber = 3294658;
    public const string DefaultSourceArchiveUrl = "https://developers.eveonline.com/static-data/tranquility/eve-online-static-data-3294658-jsonl.zip";

    public long SupportedBuildNumber { get; init; } = DefaultSupportedBuildNumber;

    public string SourceArchiveUrl { get; init; } = DefaultSourceArchiveUrl;

    public long? ImportedBuildNumber { get; init; }

    public DateTimeOffset? ImportedAtUtc { get; init; }
}