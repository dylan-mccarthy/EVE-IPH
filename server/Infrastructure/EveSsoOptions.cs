namespace server.Infrastructure;

public sealed class EveSsoOptions
{
    public const string LegacyClientId = "2737513b64854fa0a309e125419f8eff"; // Legacy IPH client id

    public string Authority { get; set; } = "https://login.eveonline.com";
    public string ClientId { get; set; } = LegacyClientId;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = "http://localhost:5173/auth/callback";
    public string Scopes { get; set; } = "esi-skills.read_skills.v1";
}
