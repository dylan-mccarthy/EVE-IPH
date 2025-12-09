using System.Reflection;

namespace server.Infrastructure;

public sealed record AppInfo(string Name, string Environment, string Version, DateTimeOffset StartedUtc)
{
    public static AppInfo Create(IConfiguration configuration, IWebHostEnvironment env)
    {
        var assembly = Assembly.GetExecutingAssembly().GetName();
        var version = configuration["App:Version"] ?? assembly.Version?.ToString() ?? "0.0.0";
        var name = configuration["App:Name"] ?? assembly.Name ?? "EVE-IPH-API";

        return new AppInfo(name, env.EnvironmentName, version, DateTimeOffset.UtcNow);
    }
}
