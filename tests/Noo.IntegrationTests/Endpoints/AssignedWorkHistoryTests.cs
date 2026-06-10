using System.Net;
using System.Text.Json;
using FluentAssertions;
using Noo.Api.AssignedWorks.Types;

namespace Noo.IntegrationTests.Endpoints;

public class AssignedWorkHistoryTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AssignedWorkHistoryTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<Ulid> SeedAssignedWorkWithHistoryAsync(int entries)
    {
        var studentId = TestDataHelpers.GetUserId(_factory, "student");
        var mentorId = TestDataHelpers.GetUserId(_factory, "mentor");
        var assignedWorkId = await TestDataHelpers.CreateAssignedWorkAsync(_factory, studentId, mentorId);

        await TestDataHelpers.AddHistoryEntryAsync(_factory, assignedWorkId, AssignedWorkHistoryType.Created, studentId);
        for (var i = 1; i < entries; i++)
        {
            await TestDataHelpers.AddHistoryEntryAsync(_factory, assignedWorkId, AssignedWorkHistoryType.Solved, studentId);
        }

        return assignedWorkId;
    }

    [Fact(DisplayName = "GET /assigned-work/{id}/history as teacher returns 200 with entries and meta")]
    public async Task Get_History_AsTeacher_ReturnsEntries()
    {
        using var client = _factory.CreateClient();
        var assignedWorkId = await SeedAssignedWorkWithHistoryAsync(2);

        var resp = await client.AsTeacher().GetAsync($"/assigned-work/{assignedWorkId}/history");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var items = doc.RootElement.GetProperty("data").EnumerateArray().ToList();
        items.Should().HaveCount(2);
        items.Should().OnlyContain(e => e.GetProperty("_entityName").GetString() == "AssignedWorkHistory");
        doc.RootElement.GetProperty("meta").GetProperty("total").GetInt32().Should().Be(2);
    }

    [Fact(DisplayName = "GET /assigned-work/{id}/history includes changedBy user")]
    public async Task Get_History_IncludesChangedBy()
    {
        using var client = _factory.CreateClient();
        var assignedWorkId = await SeedAssignedWorkWithHistoryAsync(1);

        var resp = await client.AsAdmin().GetAsync($"/assigned-work/{assignedWorkId}/history");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var entry = doc.RootElement.GetProperty("data").EnumerateArray().Single();
        entry.GetProperty("changedBy").ValueKind.Should().NotBe(JsonValueKind.Null);
        entry.GetProperty("changedBy").GetProperty("username").GetString().Should().Be("student");
    }

    [Fact(DisplayName = "GET /assigned-work/{id}/history respects pagination")]
    public async Task Get_History_Paginates()
    {
        using var client = _factory.CreateClient();
        var assignedWorkId = await SeedAssignedWorkWithHistoryAsync(3);

        var resp = await client.AsTeacher().GetAsync($"/assigned-work/{assignedWorkId}/history?page=1&perPage=2");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("data").GetArrayLength().Should().Be(2);
        doc.RootElement.GetProperty("meta").GetProperty("total").GetInt32().Should().Be(3);
    }

    [Fact(DisplayName = "GET /assigned-work/{id}/history for work without history returns empty")]
    public async Task Get_History_NoEntries_ReturnsEmpty()
    {
        using var client = _factory.CreateClient();
        var studentId = TestDataHelpers.GetUserId(_factory, "student");
        var mentorId = TestDataHelpers.GetUserId(_factory, "mentor");
        var assignedWorkId = await TestDataHelpers.CreateAssignedWorkAsync(_factory, studentId, mentorId);

        var resp = await client.AsTeacher().GetAsync($"/assigned-work/{assignedWorkId}/history");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("data").GetArrayLength().Should().Be(0);
        doc.RootElement.GetProperty("meta").GetProperty("total").GetInt32().Should().Be(0);
    }

    [Fact(DisplayName = "GET /assigned-work/{id}/history without auth returns 401 Unauthorized")]
    public async Task Get_History_WithoutAuth_Unauthorized()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync($"/assigned-work/{Ulid.NewUlid()}/history");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
