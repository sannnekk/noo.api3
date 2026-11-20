using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Response;
using Noo.Api.Notifications.DTO;
using Noo.Api.Notifications.Models;

namespace Noo.IntegrationTests.Endpoints;

public class PushSubscriptionTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public PushSubscriptionTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static object NewSubscription(string? endpoint = null) => new
    {
        endpoint = endpoint ?? $"https://push.example.com/send/{Ulid.NewUlid()}",
        keys = new { p256dh = "BNcRdreALRFXTkOOUHK1EtK2wtaz5Ry4YfYCA_0QTpQtUbVlUls0VJXg7A8u-Ts1XbjhazAkj7I99e8QcYP7DkM", auth = "tBHItJI5svbpez7KI4CCXg" },
        userAgent = "Mozilla/5.0 (X11; Linux x86_64) Chrome/130.0"
    };

    private List<PushSubscriptionModel> SubscriptionsFor(string endpoint)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
        return db.GetDbSet<PushSubscriptionModel>().Where(s => s.Endpoint == endpoint).ToList();
    }

    [Fact(DisplayName = "GET /push-subscription/vapid-public-key reports push disabled in tests")]
    public async Task Get_PublicKey_ReturnsDisabled()
    {
        using var client = _factory.CreateClient().AsStudent();

        var resp = await client.GetAsync("/push-subscription/vapid-public-key");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await resp.Content.ReadFromJsonAsync<ApiResponseDTO<WebPushPublicKeyDTO>>();
        payload!.Data!.Enabled.Should().BeFalse();
        payload.Data.PublicKey.Should().BeNull();
    }

    [Fact(DisplayName = "POST /push-subscription without auth returns 401")]
    public async Task Subscribe_WithoutAuth_Returns401()
    {
        using var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/push-subscription", NewSubscription());
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /push-subscription then GET lists the device")]
    public async Task Subscribe_Then_List()
    {
        using var client = _factory.CreateClient().AsStudent();
        var endpoint = $"https://push.example.com/send/{Ulid.NewUlid()}";

        var create = await client.PostAsJsonAsync("/push-subscription", NewSubscription(endpoint));
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var list = await client.GetFromJsonAsync<ApiResponseDTO<IEnumerable<PushSubscriptionDTO>>>("/push-subscription");
        list!.Data.Should().Contain(s => s.Endpoint == endpoint);
    }

    [Fact(DisplayName = "POST /push-subscription with a non-https endpoint returns 400")]
    public async Task Subscribe_HttpEndpoint_BadRequest()
    {
        using var client = _factory.CreateClient().AsStudent();

        var resp = await client.PostAsJsonAsync("/push-subscription", NewSubscription("http://push.example.com/x"));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST /push-subscription with a known endpoint moves it to the caller")]
    public async Task Subscribe_KnownEndpoint_IsReassigned()
    {
        var endpoint = $"https://push.example.com/send/{Ulid.NewUlid()}";
        var teacherId = TestDataHelpers.GetUserId(_factory, "teacher");

        using (var student = _factory.CreateClient().AsStudent())
        {
            (await student.PostAsJsonAsync("/push-subscription", NewSubscription(endpoint)))
                .StatusCode.Should().Be(HttpStatusCode.Created);
        }

        using (var teacher = _factory.CreateClient().AsTeacher(teacherId))
        {
            (await teacher.PostAsJsonAsync("/push-subscription", NewSubscription(endpoint)))
                .StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var rows = SubscriptionsFor(endpoint);
        rows.Should().ContainSingle();
        rows[0].UserId.Should().Be(teacherId);
    }

    [Fact(DisplayName = "DELETE /push-subscription?endpoint= unsubscribes the device")]
    public async Task Unsubscribe_ByEndpoint()
    {
        using var client = _factory.CreateClient().AsStudent();
        var endpoint = $"https://push.example.com/send/{Ulid.NewUlid()}";
        await client.PostAsJsonAsync("/push-subscription", NewSubscription(endpoint));

        var resp = await client.DeleteAsync($"/push-subscription?endpoint={Uri.EscapeDataString(endpoint)}");
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        SubscriptionsFor(endpoint).Should().BeEmpty();
    }

    [Fact(DisplayName = "DELETE /push-subscription/{id} cannot remove another user's device")]
    public async Task Delete_OthersDevice_IsNoop()
    {
        var endpoint = $"https://push.example.com/send/{Ulid.NewUlid()}";

        using var student = _factory.CreateClient().AsStudent();
        await student.PostAsJsonAsync("/push-subscription", NewSubscription(endpoint));
        var id = SubscriptionsFor(endpoint).Single().Id;

        using var teacher = _factory.CreateClient().AsTeacher();
        var resp = await teacher.DeleteAsync($"/push-subscription/{id}");
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        SubscriptionsFor(endpoint).Should().ContainSingle();
    }

    [Fact(DisplayName = "POST /push-subscription/test returns 204 while push is disabled")]
    public async Task SendTest_Disabled_NoContent()
    {
        using var client = _factory.CreateClient().AsStudent();

        var resp = await client.PostAsync("/push-subscription/test", null);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
