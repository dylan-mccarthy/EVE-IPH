using System.Security.Cryptography;
using System.Text;

namespace EVE.IPH.Infrastructure.ESI.Authentication;

/// <summary>
/// Generates PKCE verifier and challenge pairs for EVE SSO.
/// </summary>
public sealed class EsiPkceGenerator
{
    public EsiPkceChallenge Generate()
    {
        byte[] verifierBytes = RandomNumberGenerator.GetBytes(32);
        string verifier = Base64UrlEncode(verifierBytes);

        byte[] challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        string challenge = Base64UrlEncode(challengeBytes);

        return new EsiPkceChallenge(verifier, challenge);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}