using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Security;
using Noo.Api.GoogleSheetsIntegrations.Types;

namespace Noo.UnitTests.GoogleSheetsIntegrations;

public class GoogleSheetsIntegrationScheduleTests
{
    private static readonly DateTime _origin = new(2026, 8, 13, 12, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public void Manual_Never_Schedules_Itself()
    {
        Assert.Null(GoogleSheetsIntegrationSchedule.Manual.NextRunAt(_origin));
    }

    [Theory]
    [InlineData(GoogleSheetsIntegrationSchedule.Hourly, 1)]
    [InlineData(GoogleSheetsIntegrationSchedule.Daily, 24)]
    [InlineData(GoogleSheetsIntegrationSchedule.Weekly, 24 * 7)]
    public void Recurring_Schedules_Advance_By_Their_Period(
        GoogleSheetsIntegrationSchedule schedule,
        int expectedHours
    )
    {
        Assert.Equal(_origin.AddHours(expectedHours), schedule.NextRunAt(_origin));
    }
}

public class AesGcmSecretProtectorTests
{
    private static AesGcmSecretProtector CreateProtector(string? key = null)
    {
        return new AesGcmSecretProtector(
            Options.Create(
                new GoogleConfig
                {
                    ClientId = "id",
                    ClientSecret = "secret",
                    RedirectUri = "https://example.test/callback",
                    TokenEncryptionKey =
                        key ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                }
            )
        );
    }

    [Fact]
    public void Round_Trips_A_Secret()
    {
        var protector = CreateProtector();
        const string secret = "1//0abcdefgRefreshToken_ЮникодТоже";

        Assert.Equal(secret, protector.Unprotect(protector.Protect(secret)));
    }

    [Fact]
    public void Produces_A_Different_Ciphertext_Every_Time()
    {
        var protector = CreateProtector();

        // A fresh nonce per call means the same token never encrypts to the same
        // string twice, so stored values cannot be compared to spot reuse.
        Assert.NotEqual(protector.Protect("same"), protector.Protect("same"));
    }

    [Fact]
    public void Rejects_A_Value_Encrypted_Under_A_Different_Key()
    {
        var protectedValue = CreateProtector().Protect("secret");
        var otherProtector = CreateProtector();

        // Surfaces as AuthenticationTagMismatchException, a CryptographicException subtype.
        Assert.ThrowsAny<CryptographicException>(
            () => otherProtector.Unprotect(protectedValue)
        );
    }

    [Fact]
    public void Rejects_A_Tampered_Value()
    {
        var protector = CreateProtector();
        var protectedValue = protector.Protect("secret");

        var bytes = Convert.FromBase64String(protectedValue);
        bytes[^1] ^= 0xFF;

        Assert.ThrowsAny<CryptographicException>(
            () => protector.Unprotect(Convert.ToBase64String(bytes))
        );
    }

    [Fact]
    public void Rejects_Garbage()
    {
        var protector = CreateProtector();

        Assert.Throws<CryptographicException>(() => protector.Unprotect("not base64!!"));
        Assert.Throws<CryptographicException>(() => protector.Unprotect("c2hvcnQ="));
    }

    [Fact]
    public void Refuses_A_Key_That_Is_Not_A_256_Bit_Base64_Value()
    {
        Assert.Throws<InvalidOperationException>(() => CreateProtector("too-short"));
        Assert.Throws<InvalidOperationException>(
            () => CreateProtector(Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)))
        );
    }
}
