using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Works.Models;
using Noo.Api.Works.Types;

namespace Noo.IntegrationTests.Endpoints;

/// <summary>
/// A work checked by hand is worth what its mentor gave its answers. Nothing
/// works that out until the check is sent: the score a work carries while it is
/// being checked is only what could be worked out automatically when it was
/// handed in — nothing at all for a work whose tasks are all checked by hand.
/// </summary>
public class AssignedWorkCheckScoreTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AssignedWorkCheckScoreTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private sealed record Seeded(Ulid AssignedWorkId, Ulid MentorId, Ulid StudentId, Ulid[] TaskIds);

    /// <summary>
    /// A handed-in work of three essay tasks — none of them automatically
    /// checkable — carrying an unscored answer to each.
    /// </summary>
    private async Task<Seeded> SeedSubmittedWorkAsync(int taskCount = 3, int maxScorePerTask = 5)
    {
        var studentId = TestDataHelpers.GetUserId(_factory, "student");
        var mentorId = TestDataHelpers.GetUserId(_factory, "mentor");
        var assignedWorkId = await TestDataHelpers.CreateAssignedWorkAsync(
            _factory,
            studentId,
            mentorId,
            solveStatus: AssignedWorkSolveStatus.SolvedInDeadline
        );

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();

        var work = new WorkModel
        {
            Title = "W",
            Type = WorkType.Test,
            MaxScore = taskCount * maxScorePerTask,
        };
        db.GetDbSet<WorkModel>().Add(work);

        var taskIds = new List<Ulid>();

        for (var order = 0; order < taskCount; order++)
        {
            var task = new WorkTaskModel
            {
                Content = RichTextFactory.Create("q"),
                Type = WorkTaskType.Essay,
                CheckStrategy = WorkTaskCheckStrategy.Manual,
                Order = order,
                MaxScore = maxScorePerTask,
                WorkId = work.Id,
            };
            db.GetDbSet<WorkTaskModel>().Add(task);
            taskIds.Add(task.Id);

            db.GetDbSet<AssignedWorkAnswerModel>().Add(new AssignedWorkAnswerModel
            {
                AssignedWorkId = assignedWorkId,
                TaskId = task.Id,
                RichTextContent = RichTextFactory.Create("an answer"),
                MaxScore = maxScorePerTask,
                Status = AssignedWorkAnswerStatus.Submitted,
            });
        }

        var assignedWork = await db.GetDbSet<AssignedWorkModel>().FindAsync(assignedWorkId);
        assignedWork!.WorkId = work.Id;
        assignedWork.MaxScore = work.MaxScore;
        // The helper only sets the status; being solved is having a date on it.
        assignedWork.SolvedAt = DateTime.UtcNow.AddHours(-1);
        assignedWork.Score = null;
        await db.SaveChangesAsync();

        return new Seeded(assignedWorkId, mentorId, studentId, [.. taskIds]);
    }

    private async Task ScoreAnswerAsync(
        HttpClient client,
        Seeded seeded,
        Ulid taskId,
        int score,
        int maxScore = 5
    )
    {
        // Sent as raw JSON: a Ulid only serializes to the string the API expects
        // through the converter the API registers, which these options lack.
        var body = $$"""
            {
              "taskId": "{{taskId}}",
              "status": "submitted",
              "score": {{score}},
              "maxScore": {{maxScore}}
            }
            """;

        var response = await client
            .AsMentor(seeded.MentorId)
            .PostAsync(
                $"/assigned-work/{seeded.AssignedWorkId}/save-answer",
                new StringContent(body, Encoding.UTF8, "application/json")
            );

        response.IsSuccessStatusCode.Should().BeTrue(
            "the mentor must be able to score an answer, got {0}",
            response.StatusCode
        );
    }

    private async Task<AssignedWorkModel> ReadWorkAsync(Ulid assignedWorkId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();

        return await db.GetDbSet<AssignedWorkModel>().FindAsync(assignedWorkId)
            ?? throw new InvalidOperationException("the work went missing");
    }

    [Fact(DisplayName = "sending the check totals up what the mentor gave the answers")]
    public async Task Marking_Checked_Totals_The_Scores_Given()
    {
        using var client = _factory.CreateClient();
        var seeded = await SeedSubmittedWorkAsync();

        await ScoreAnswerAsync(client, seeded, seeded.TaskIds[0], 5);
        await ScoreAnswerAsync(client, seeded, seeded.TaskIds[1], 3);
        await ScoreAnswerAsync(client, seeded, seeded.TaskIds[2], 0);

        // Nothing has totalled them up yet.
        (await ReadWorkAsync(seeded.AssignedWorkId)).Score.Should().BeNull();

        var response = await client
            .AsMentor(seeded.MentorId)
            .PostAsync($"/assigned-work/{seeded.AssignedWorkId}/mark-checked", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var work = await ReadWorkAsync(seeded.AssignedWorkId);

        work.Score.Should().Be(8);
        work.CheckedAt.Should().NotBeNull();
    }

    [Fact(DisplayName = "the checked work carries that score to whoever reads it back")]
    public async Task The_Score_Reaches_The_Student_Reading_The_Checked_Work()
    {
        using var client = _factory.CreateClient();
        var seeded = await SeedSubmittedWorkAsync();

        await ScoreAnswerAsync(client, seeded, seeded.TaskIds[0], 4);
        await ScoreAnswerAsync(client, seeded, seeded.TaskIds[1], 2);

        await client
            .AsMentor(seeded.MentorId)
            .PostAsync($"/assigned-work/{seeded.AssignedWorkId}/mark-checked", null);

        var response = await client
            .AsUserId(seeded.StudentId)
            .GetAsync($"/assigned-work/{seeded.AssignedWorkId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        doc.RootElement.GetProperty("data").GetProperty("score").GetInt32().Should().Be(6);
    }

    // A mentor who leaves a task alone has given it no marks, rather than an
    // unknown number of them.
    [Fact(DisplayName = "an answer left unscored counts as nothing towards the total")]
    public async Task An_Unscored_Answer_Counts_As_Zero()
    {
        using var client = _factory.CreateClient();
        var seeded = await SeedSubmittedWorkAsync();

        await ScoreAnswerAsync(client, seeded, seeded.TaskIds[0], 5);

        await client
            .AsMentor(seeded.MentorId)
            .PostAsync($"/assigned-work/{seeded.AssignedWorkId}/mark-checked", null);

        (await ReadWorkAsync(seeded.AssignedWorkId)).Score.Should().Be(5);
    }

    [Fact(DisplayName = "a work checked with nothing given at all scores zero, not null")]
    public async Task A_Work_Given_Nothing_Scores_Zero()
    {
        using var client = _factory.CreateClient();
        var seeded = await SeedSubmittedWorkAsync();

        await client
            .AsMentor(seeded.MentorId)
            .PostAsync($"/assigned-work/{seeded.AssignedWorkId}/mark-checked", null);

        (await ReadWorkAsync(seeded.AssignedWorkId)).Score.Should().Be(0);
    }

    [Fact(DisplayName = "re-checking a work totals up the scores as they stand then")]
    public async Task Re_Checking_Totals_The_Scores_Again()
    {
        using var client = _factory.CreateClient();
        var seeded = await SeedSubmittedWorkAsync();

        await ScoreAnswerAsync(client, seeded, seeded.TaskIds[0], 5);
        await client
            .AsMentor(seeded.MentorId)
            .PostAsync($"/assigned-work/{seeded.AssignedWorkId}/mark-checked", null);

        (await ReadWorkAsync(seeded.AssignedWorkId)).Score.Should().Be(5);

        var returned = await client
            .AsMentor(seeded.MentorId)
            .PatchAsync($"/assigned-work/{seeded.AssignedWorkId}/return-to-check", null);
        returned.IsSuccessStatusCode.Should().BeTrue();

        await ScoreAnswerAsync(client, seeded, seeded.TaskIds[1], 4);

        await client
            .AsMentor(seeded.MentorId)
            .PostAsync($"/assigned-work/{seeded.AssignedWorkId}/mark-checked", null);

        (await ReadWorkAsync(seeded.AssignedWorkId)).Score.Should().Be(9);
    }
}
