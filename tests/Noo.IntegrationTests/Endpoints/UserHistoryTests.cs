using System.Net;
using System.Text.Json;
using FluentAssertions;
using Noo.Api.UserHistory.Types;

namespace Noo.IntegrationTests.Endpoints;

public class UserHistoryTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public UserHistoryTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static List<JsonElement> ItemsOf(string body)
    {
        return JsonDocument
            .Parse(body)
            .RootElement.GetProperty("data")
            .EnumerateArray()
            .ToList();
    }

    /// <summary>
    /// A user of their own, so entries seeded here cannot collide with those of another test
    /// sharing the fixture.
    /// </summary>
    private Task<Ulid> CreateSubjectAsync()
    {
        var name = $"subject-{Ulid.NewUlid()}";

        return TestDataHelpers.CreateUserAsync(
            _factory,
            name,
            $"{name}@example.com",
            "Password1!"
        );
    }

    [Fact(DisplayName = "GET /user/{id}/history as teacher returns 200 with entries and meta")]
    public async Task Get_History_AsTeacher_ReturnsEntries()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync();

        await TestDataHelpers.AddUserHistoryEntryAsync(
            _factory,
            subjectId,
            UserHistoryType.Registered
        );
        await TestDataHelpers.AddUserHistoryEntryAsync(
            _factory,
            subjectId,
            UserHistoryType.Verified,
            TestDataHelpers.GetUserId(_factory, "admin")
        );

        var resp = await client.AsTeacher().GetAsync($"/user/{subjectId}/history");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadAsStringAsync();
        var items = ItemsOf(body);

        items.Should().HaveCount(2);
        items.Should().OnlyContain(e => e.GetProperty("_entityName").GetString() == "UserHistory");
        JsonDocument
            .Parse(body)
            .RootElement.GetProperty("meta")
            .GetProperty("total")
            .GetInt32()
            .Should()
            .Be(2);
    }

    [Fact(DisplayName = "GET /user/{id}/history serializes the type in kebab-case")]
    public async Task Get_History_SerializesTypeAsKebabCase()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync();

        await TestDataHelpers.AddUserHistoryEntryAsync(
            _factory,
            subjectId,
            UserHistoryType.AddedToCourse,
            payload: new Dictionary<string, string>
            {
                ["courseId"] = Ulid.NewUlid().ToString(),
                ["courseName"] = "Физика ЕГЭ",
            }
        );

        var resp = await client.AsAdmin().GetAsync($"/user/{subjectId}/history");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var entry = ItemsOf(await resp.Content.ReadAsStringAsync()).Single();

        entry.GetProperty("type").GetString().Should().Be("added-to-course");
        entry.GetProperty("payload").GetProperty("courseName").GetString().Should().Be("Физика ЕГЭ");
    }

    [Fact(DisplayName = "GET /user/{id}/history includes the actor user")]
    public async Task Get_History_IncludesActor()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync();
        var actorId = TestDataHelpers.GetUserId(_factory, "admin");

        await TestDataHelpers.AddUserHistoryEntryAsync(
            _factory,
            subjectId,
            UserHistoryType.Blocked,
            actorId
        );

        var resp = await client.AsAdmin().GetAsync($"/user/{subjectId}/history");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var entry = ItemsOf(await resp.Content.ReadAsStringAsync()).Single();

        entry.GetProperty("actor").ValueKind.Should().NotBe(JsonValueKind.Null);
        entry.GetProperty("actor").GetProperty("username").GetString().Should().Be("admin");
    }

    [Fact(DisplayName = "GET /user/{id}/history?perspective=actor returns what the user did")]
    public async Task Get_History_ActorPerspective_ReturnsPerformedActions()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync();
        var otherSubjectId = await CreateSubjectAsync();

        // Happened to the subject.
        await TestDataHelpers.AddUserHistoryEntryAsync(
            _factory,
            subjectId,
            UserHistoryType.Registered
        );

        // Performed by the subject on someone else.
        await TestDataHelpers.AddUserHistoryEntryAsync(
            _factory,
            otherSubjectId,
            UserHistoryType.Verified,
            subjectId
        );

        var asSubject = await client.AsTeacher().GetAsync($"/user/{subjectId}/history");
        ItemsOf(await asSubject.Content.ReadAsStringAsync())
            .Should()
            .ContainSingle()
            .Which.GetProperty("type")
            .GetString()
            .Should()
            .Be("registered");

        var asActor = await client
            .AsTeacher()
            .GetAsync($"/user/{subjectId}/history?perspective=actor");
        asActor.StatusCode.Should().Be(HttpStatusCode.OK, await asActor.Content.ReadAsStringAsync());
        ItemsOf(await asActor.Content.ReadAsStringAsync())
            .Should()
            .ContainSingle()
            .Which.GetProperty("subjectUserId")
            .GetString()
            .Should()
            .Be(otherSubjectId.ToString());
    }

    [Fact(DisplayName = "GET /user/{id}/history filters by type")]
    public async Task Get_History_FiltersByType()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync();

        await TestDataHelpers.AddUserHistoryEntryAsync(
            _factory,
            subjectId,
            UserHistoryType.Registered
        );
        await TestDataHelpers.AddUserHistoryEntryAsync(
            _factory,
            subjectId,
            UserHistoryType.AddedToCourse
        );

        // A multi-word type in kebab-case: that is the form the API hands out, so it is the form
        // the frontend sends back, and query-string binding does not go through the JSON converter.
        var resp = await client
            .AsTeacher()
            .GetAsync($"/user/{subjectId}/history?type=added-to-course");
        var body = await resp.Content.ReadAsStringAsync();
        resp.StatusCode.Should().Be(HttpStatusCode.OK, body);

        ItemsOf(body)
            .Should()
            .ContainSingle()
            .Which.GetProperty("type")
            .GetString()
            .Should()
            .Be("added-to-course");
    }

    [Fact(DisplayName = "GET /user/{id}/history respects pagination")]
    public async Task Get_History_Paginates()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync();

        for (var i = 0; i < 3; i++)
        {
            await TestDataHelpers.AddUserHistoryEntryAsync(
                _factory,
                subjectId,
                UserHistoryType.Registered
            );
        }

        var resp = await client.AsTeacher().GetAsync($"/user/{subjectId}/history?page=1&perPage=2");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("data").GetArrayLength().Should().Be(2);
        doc.RootElement.GetProperty("meta").GetProperty("total").GetInt32().Should().Be(3);
    }

    [Fact(DisplayName = "GET /user/{id}/history for a user without history returns empty")]
    public async Task Get_History_NoEntries_ReturnsEmpty()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync();

        var resp = await client.AsTeacher().GetAsync($"/user/{subjectId}/history");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("data").GetArrayLength().Should().Be(0);
        doc.RootElement.GetProperty("meta").GetProperty("total").GetInt32().Should().Be(0);
    }

    [Fact(DisplayName = "A real action records a history entry once its request has committed")]
    public async Task RealAction_RecordsHistoryEntry()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync();

        // Act as the seeded teacher rather than an anonymous role token, so the recorded actor
        // resolves to a real user and the attribution can be asserted.
        var teacherId = TestDataHelpers.GetUserId(_factory, "teacher");

        var verify = await client
            .AsTeacher(teacherId)
            .PatchAsync($"/user/{subjectId}/verify-manual", null);
        verify.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // The event is dispatched by a background pump after the request's unit of work commits,
        // so the entry lands shortly after the response rather than during it.
        var deadline = DateTime.UtcNow.AddSeconds(10);
        List<JsonElement> items;

        do
        {
            var resp = await client.AsTeacher().GetAsync($"/user/{subjectId}/history");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            items = ItemsOf(await resp.Content.ReadAsStringAsync());

            if (items.Count > 0)
            {
                break;
            }

            await Task.Delay(100);
        } while (DateTime.UtcNow < deadline);

        var entry = items.Should().ContainSingle().Subject;
        entry.GetProperty("type").GetString().Should().Be("verified");
        entry.GetProperty("subjectUserId").GetString().Should().Be(subjectId.ToString());
        entry.GetProperty("actor").GetProperty("username").GetString().Should().Be("teacher");
    }

    [Fact(DisplayName = "GET /user/{id}/history as student returns 403 Forbidden")]
    public async Task Get_History_AsStudent_Forbidden()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync();

        var resp = await client.AsStudent().GetAsync($"/user/{subjectId}/history");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "GET /user/{id}/history without auth returns 401 Unauthorized")]
    public async Task Get_History_WithoutAuth_Unauthorized()
    {
        using var client = _factory.CreateClient();

        var resp = await client.GetAsync($"/user/{Ulid.NewUlid()}/history");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
