using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Noo.Api.Auth.External.Services;

namespace Noo.UnitTests.Auth.External;

public class PkceTests
{
    [Fact]
    public void Verifier_Is_Within_The_Rfc7636_Length_Range()
    {
        var verifier = Pkce.CreateVerifier();

        Assert.InRange(verifier.Length, 43, 128);
    }

    [Fact]
    public void Verifier_Uses_Only_Unreserved_Characters()
    {
        for (var i = 0; i < 50; i++)
        {
            var verifier = Pkce.CreateVerifier();

            Assert.All(
                verifier,
                character =>
                    Assert.True(
                        char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.' or '~',
                        $"Unexpected character '{character}' in verifier."
                    )
            );
        }
    }

    [Fact]
    public void Verifiers_Are_Not_Repeated()
    {
        var verifiers = Enumerable.Range(0, 100).Select(_ => Pkce.CreateVerifier()).ToHashSet();

        Assert.Equal(100, verifiers.Count);
    }

    [Fact]
    public void Challenge_Is_Unpadded_Base64Url_Of_The_Sha256_Digest()
    {
        var verifier = Pkce.CreateVerifier();

        var challenge = Pkce.CreateChallenge(verifier);

        var expected = WebEncoders.Base64UrlEncode(
            SHA256.HashData(Encoding.ASCII.GetBytes(verifier))
        );

        Assert.Equal(expected, challenge);
        Assert.DoesNotContain('=', challenge);
        Assert.DoesNotContain('+', challenge);
        Assert.DoesNotContain('/', challenge);
    }

    [Fact]
    public void Challenge_Matches_The_Rfc7636_Reference_Vector()
    {
        // RFC 7636 appendix B.
        var challenge = Pkce.CreateChallenge("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk");

        Assert.Equal("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM", challenge);
    }
}
