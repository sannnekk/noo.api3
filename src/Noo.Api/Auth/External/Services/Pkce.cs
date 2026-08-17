using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Noo.Api.Core.Utils;

namespace Noo.Api.Auth.External.Services;

/// <summary>
/// PKCE is mandatory for VK ID and optional for Yandex, so it is always on — that removes
/// the need for a per-provider capability flag.
/// </summary>
public static class Pkce
{
    /// <summary>32 bytes gives 43 Base64Url characters, the minimum RFC 7636 allows.</summary>
    public static string CreateVerifier() => RandomGenerator.GenerateRandomUrlToken(32);

    public static string CreateChallenge(string verifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));

        return WebEncoders.Base64UrlEncode(hash);
    }
}
