using Noo.Api.Auth.External.Services;
using Noo.Api.Auth.External.Types;
using Noo.Api.Core.DataAbstraction.Cache;

namespace Noo.UnitTests.Auth.External;

public class ExternalAuthChallengeStoreTests
{
    private static ExternalAuthChallenge Challenge(string state) =>
        new()
        {
            Provider = ExternalAuthProviderType.Vk,
            Intent = ExternalAuthIntent.Link,
            State = state,
            CodeVerifier = "verifier",
            RedirectUri = "http://localhost/auth/callback/vk",
            ReturnUrl = "/settings/connected-accounts",
            UserId = Ulid.NewUlid(),
        };

    // Round-trips through the real cache serializer, so a type the cache cannot represent
    // (Ulid, the enums) fails here rather than in production.
    [Fact]
    public async Task Redeem_Returns_The_Saved_Challenge_Intact()
    {
        var store = new ExternalAuthChallengeStore(new MemoryCacheRepository());
        var challenge = Challenge("state-1");

        await store.SaveAsync(challenge);

        Assert.Equal(challenge, await store.RedeemAsync("state-1"));
    }

    [Fact]
    public async Task Redeem_Consumes_The_Challenge()
    {
        var store = new ExternalAuthChallengeStore(new MemoryCacheRepository());

        await store.SaveAsync(Challenge("state-2"));
        await store.RedeemAsync("state-2");

        Assert.Null(await store.RedeemAsync("state-2"));
    }

    [Fact]
    public async Task Redeem_Returns_Null_For_An_Unknown_State()
    {
        var store = new ExternalAuthChallengeStore(new MemoryCacheRepository());

        Assert.Null(await store.RedeemAsync("never-issued"));
        Assert.Null(await store.RedeemAsync(string.Empty));
    }
}
