using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Security;

namespace Noo.UnitTests.Core.Security;

public class HashServiceTests
{
    [Fact]
    public void Hash_Verify_Works_ForCorrectAndIncorrectInputs()
    {
        var svc = new HashService(Options.Create(new AppConfig
        {
            Location = "test",
            BaseUrl = "http://localhost",
            UserOnlineThresholdMinutes = 15,
            UserActiveThresholdDays = 14,
            HashSecret = "secret",
            AllowedOrigins = new[] { "*" }
        }));
        var hash = svc.Hash("abc");

        // Should verify for correct input
        Assert.True(svc.Verify("abc", hash));

        // Should fail for incorrect input
        Assert.False(svc.Verify("abcd", hash));
    }
}
