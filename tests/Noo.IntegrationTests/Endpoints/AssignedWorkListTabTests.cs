using System.Net;
using System.Text.Json;
using FluentAssertions;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.Security.Authorization;

namespace Noo.IntegrationTests.Endpoints;

/// <summary>
/// The tab counters of the assigned work list and the list itself have to answer the same
/// question: a tab whose counter says three has to show three works. These tests seed every
/// status a work can be in and compare, tab by tab, the counter from the metadata endpoint
/// with the total the list endpoint reports for that tab.
/// </summary>
public class AssignedWorkListTabTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    /// <summary>
    /// Tab as the client asks for it, paired with the counter shown on it.
    /// </summary>
    private static readonly (string Tab, string Counter)[] TabsWithCounters =
    [
        ("all", "all"),
        ("not-solved", "notSolved"),
        ("not-checked", "notChecked"),
        ("checked", "checked"),
    ];

    public AssignedWorkListTabTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact(DisplayName = "Every tab counter of a student's list equals the number of works that tab shows")]
    public async Task TabCounters_MatchListTotals_ForStudent()
    {
        using var client = _factory.CreateClient();
        var studentId = await CreateUserAsync(UserRoles.Student);
        var mentorId = TestDataHelpers.GetUserId(_factory, "mentor");

        await SeedWorkInEveryStatusAsync(studentId, mentorId);

        var counts = await GetCountsAsync(client.AsUserId(studentId), studentId);

        // The eight seeded works, sliced the way the list page slices them. Both "solved"
        // statuses count as handed in and all three "checked" ones as checked.
        counts["all"].Should().Be(8);
        counts["notSolved"].Should().Be(2);
        counts["notChecked"].Should().Be(3);
        counts["checked"].Should().Be(3);

        foreach (var (tab, counter) in TabsWithCounters)
        {
            var (total, returned) = await GetTabAsync(client.AsUserId(studentId), tab);

            total.Should().Be(counts[counter], $"the '{tab}' tab counter must match its total");
            returned.Should().Be(counts[counter], $"the '{tab}' tab counter must match the works it shows");
        }
    }

    [Fact(DisplayName = "Every tab counter of a mentor's list equals the number of works that tab shows")]
    public async Task TabCounters_MatchListTotals_ForMentor()
    {
        using var client = _factory.CreateClient();
        var mentorId = await CreateUserAsync(UserRoles.Mentor);
        var studentId = TestDataHelpers.GetUserId(_factory, "student");
        var otherMentorId = TestDataHelpers.GetUserId(_factory, "mentor");

        await SeedWorkInEveryStatusAsync(studentId, mentorId);
        // A work the mentor only helps with counts for them just as much as their own.
        await TestDataHelpers.CreateAssignedWorkAsync(
            _factory,
            studentId,
            otherMentorId,
            AssignedWorkSolveStatus.SolvedInDeadline,
            AssignedWorkCheckStatus.NotChecked,
            helperMentorId: mentorId
        );

        var counts = await GetCountsAsync(client.AsMentor(mentorId), mentorId);

        counts["all"].Should().Be(9);

        foreach (var (tab, counter) in TabsWithCounters)
        {
            var (total, returned) = await GetTabAsync(client.AsMentor(mentorId), tab);

            total.Should().Be(counts[counter], $"the '{tab}' tab counter must match its total");
            returned.Should().Be(counts[counter], $"the '{tab}' tab counter must match the works it shows");
        }
    }

    [Fact(DisplayName = "A work being solved or being checked stays in the unsolved / unchecked tab")]
    public async Task Tabs_KeepWorkInProgress()
    {
        using var client = _factory.CreateClient();
        var studentId = await CreateUserAsync(UserRoles.Student);
        var mentorId = TestDataHelpers.GetUserId(_factory, "mentor");

        await TestDataHelpers.CreateAssignedWorkAsync(
            _factory,
            studentId,
            mentorId,
            AssignedWorkSolveStatus.InProgress,
            AssignedWorkCheckStatus.NotChecked
        );
        await TestDataHelpers.CreateAssignedWorkAsync(
            _factory,
            studentId,
            mentorId,
            AssignedWorkSolveStatus.SolvedInDeadline,
            AssignedWorkCheckStatus.InProgress
        );

        var (notSolvedTotal, _) = await GetTabAsync(client.AsUserId(studentId), "not-solved");
        var (notCheckedTotal, _) = await GetTabAsync(client.AsUserId(studentId), "not-checked");
        var (checkedTotal, _) = await GetTabAsync(client.AsUserId(studentId), "checked");

        notSolvedTotal.Should().Be(1);
        notCheckedTotal.Should().Be(1);
        checkedTotal.Should().Be(0);
    }

    private Task<Ulid> CreateUserAsync(UserRoles role)
    {
        var username = $"{role}-{Ulid.NewUlid()}".ToLowerInvariant();

        return TestDataHelpers.CreateUserAsync(
            _factory,
            username,
            $"{username}@example.com",
            "test",
            role
        );
    }

    private async Task SeedWorkInEveryStatusAsync(Ulid studentId, Ulid mentorId)
    {
        var statuses = new[]
        {
            (AssignedWorkSolveStatus.NotSolved, AssignedWorkCheckStatus.NotChecked),
            (AssignedWorkSolveStatus.InProgress, AssignedWorkCheckStatus.NotChecked),
            (AssignedWorkSolveStatus.SolvedInDeadline, AssignedWorkCheckStatus.NotChecked),
            (AssignedWorkSolveStatus.SolvedInDeadline, AssignedWorkCheckStatus.InProgress),
            (AssignedWorkSolveStatus.SolvedInDeadline, AssignedWorkCheckStatus.CheckedInDeadline),
            (AssignedWorkSolveStatus.SolvedAfterDeadline, AssignedWorkCheckStatus.NotChecked),
            (AssignedWorkSolveStatus.SolvedAfterDeadline, AssignedWorkCheckStatus.CheckedAfterDeadline),
            (AssignedWorkSolveStatus.SolvedInDeadline, AssignedWorkCheckStatus.CheckedAutomatically),
        };

        foreach (var (solveStatus, checkStatus) in statuses)
        {
            await TestDataHelpers.CreateAssignedWorkAsync(
                _factory,
                studentId,
                mentorId,
                solveStatus,
                checkStatus
            );
        }
    }

    private static async Task<Dictionary<string, int>> GetCountsAsync(HttpClient client, Ulid userId)
    {
        var resp = await client.GetAsync($"/assigned-work/{userId}/metadata");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());

        return doc.RootElement.GetProperty("data")
            .GetProperty("counts")
            .EnumerateObject()
            .ToDictionary(counter => counter.Name, counter => counter.Value.GetInt32());
    }

    private static async Task<(int Total, int Returned)> GetTabAsync(HttpClient client, string tab)
    {
        var resp = await client.GetAsync($"/assigned-work?tab={tab}&perPage=50");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());

        return (
            doc.RootElement.GetProperty("meta").GetProperty("total").GetInt32(),
            doc.RootElement.GetProperty("data").GetArrayLength()
        );
    }
}
