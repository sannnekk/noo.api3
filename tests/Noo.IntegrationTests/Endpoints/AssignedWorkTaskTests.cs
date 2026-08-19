using System.Net;
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

public class AssignedWorkTaskTests : IClassFixture<ApiFactory>
{
    private const string AnswerKey = "the-answer";

    private readonly ApiFactory _factory;

    public AssignedWorkTaskTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Ulid assignedWorkId, Ulid taskId, Ulid studentId)> SeedAsync(
        bool showAnswerBeforeCheck = false,
        bool checkOneByOne = false,
        string? givenAnswer = AnswerKey
    )
    {
        var studentId = TestDataHelpers.GetUserId(_factory, "student");
        var mentorId = TestDataHelpers.GetUserId(_factory, "mentor");
        var assignedWorkId = await TestDataHelpers.CreateAssignedWorkAsync(
            _factory,
            studentId,
            mentorId,
            solveStatus: AssignedWorkSolveStatus.InProgress
        );

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();

        var work = new WorkModel { Title = "W", Type = WorkType.Test, MaxScore = 10 };
        db.GetDbSet<WorkModel>().Add(work);

        var task = new WorkTaskModel
        {
            Content = RichTextFactory.Create("q"),
            Type = WorkTaskType.Word,
            CheckStrategy = WorkTaskCheckStrategy.ExactMatchOrZero,
            RightAnswers = [AnswerKey],
            Order = 0,
            MaxScore = 10,
            WorkId = work.Id,
            ShowAnswerBeforeCheck = showAnswerBeforeCheck,
            CheckOneByOne = checkOneByOne,
        };
        db.GetDbSet<WorkTaskModel>().Add(task);

        if (givenAnswer != null)
        {
            db.GetDbSet<AssignedWorkAnswerModel>().Add(new AssignedWorkAnswerModel
            {
                AssignedWorkId = assignedWorkId,
                TaskId = task.Id,
                WordContent = givenAnswer,
                MaxScore = 10,
                Status = AssignedWorkAnswerStatus.NotSubmitted,
            });
        }

        var assignedWork = await db.GetDbSet<AssignedWorkModel>().FindAsync(assignedWorkId);
        assignedWork!.WorkId = work.Id;
        await db.SaveChangesAsync();

        return (assignedWorkId, task.Id, studentId);
    }

    [Fact(DisplayName = "GET /assigned-work/{id} does not carry the answer key to the student solving it")]
    public async Task The_Work_A_Student_Is_Solving_Carries_No_Answer_Key()
    {
        using var client = _factory.CreateClient();
        // A wrong answer of their own, so the key can only come from the key.
        var (assignedWorkId, _, studentId) = await SeedAsync(givenAnswer: "my-attempt");

        var response = await client.AsUserId(studentId).GetAsync($"/assigned-work/{assignedWorkId}");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotContain(AnswerKey);
        RightAnswersOf(body).Should().BeEmpty();
    }

    [Fact(DisplayName = "the mentor checking the work still gets the answer key")]
    public async Task The_Mentor_Still_Gets_The_Answer_Key()
    {
        using var client = _factory.CreateClient();
        var (assignedWorkId, _, _) = await SeedAsync();
        var mentorId = TestDataHelpers.GetUserId(_factory, "mentor");

        var response = await client
            .AsMentor(mentorId)
            .GetAsync($"/assigned-work/{assignedWorkId}");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        RightAnswersOf(body).Should().Equal(AnswerKey);
    }

    [Fact(DisplayName = "a task that offers its answer hands it over on request")]
    public async Task Answer_Key_Is_Served_For_A_Task_That_Offers_It()
    {
        using var client = _factory.CreateClient();
        var (assignedWorkId, taskId, studentId) = await SeedAsync(showAnswerBeforeCheck: true);

        var response = await client
            .AsUserId(studentId)
            .GetAsync($"/assigned-work/{assignedWorkId}/task/{taskId}/answer-key");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("data")
            .GetProperty("rightAnswers")
            .EnumerateArray()
            .Select(e => e.GetString())
            .Should()
            .Equal(AnswerKey);
    }

    [Fact(DisplayName = "a task that does not offer its answer refuses to hand it over")]
    public async Task Answer_Key_Is_Refused_For_A_Task_That_Does_Not_Offer_It()
    {
        using var client = _factory.CreateClient();
        var (assignedWorkId, taskId, studentId) = await SeedAsync(showAnswerBeforeCheck: false);

        var response = await client
            .AsUserId(studentId)
            .GetAsync($"/assigned-work/{assignedWorkId}/task/{taskId}/answer-key");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "checking one task scores it and locks the answer")]
    public async Task Checking_One_Task_Scores_And_Locks_It()
    {
        using var client = _factory.CreateClient();
        var (assignedWorkId, taskId, studentId) = await SeedAsync(checkOneByOne: true);

        var response = await client
            .AsUserId(studentId)
            .PostAsync($"/assigned-work/{assignedWorkId}/task/{taskId}/check", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("score").GetInt32().Should().Be(10);
        data.GetProperty("isCorrect").GetBoolean().Should().BeTrue();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
        var answer = db.GetDbSet<AssignedWorkAnswerModel>()
            .First(a => a.AssignedWorkId == assignedWorkId && a.TaskId == taskId);

        answer.Status.Should().Be(AssignedWorkAnswerStatus.Checked);
        answer.Score.Should().Be(10);
    }

    [Fact(DisplayName = "a task not marked one-by-one refuses to be checked on its own")]
    public async Task Checking_A_Task_Not_Marked_One_By_One_Is_Refused()
    {
        using var client = _factory.CreateClient();
        var (assignedWorkId, taskId, studentId) = await SeedAsync(checkOneByOne: false);

        var response = await client
            .AsUserId(studentId)
            .PostAsync($"/assigned-work/{assignedWorkId}/task/{taskId}/check", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "neither endpoint answers to somebody else's work")]
    public async Task An_Outsider_Gets_Nothing()
    {
        using var client = _factory.CreateClient();
        var (assignedWorkId, taskId, _) = await SeedAsync(
            showAnswerBeforeCheck: true,
            checkOneByOne: true
        );
        var outsider = Ulid.NewUlid();

        var key = await client
            .AsUserId(outsider)
            .GetAsync($"/assigned-work/{assignedWorkId}/task/{taskId}/answer-key");
        var check = await client
            .AsUserId(outsider)
            .PostAsync($"/assigned-work/{assignedWorkId}/task/{taskId}/check", null);

        key.StatusCode.Should().Be(HttpStatusCode.NotFound);
        check.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// The answer keys of the work's tasks as the response carries them.
    /// </summary>
    private static IEnumerable<string?> RightAnswersOf(string body)
    {
        return JsonDocument.Parse(body)
            .RootElement.GetProperty("data")
            .GetProperty("work")
            .GetProperty("tasks")
            .EnumerateArray()
            .SelectMany(task =>
                task.GetProperty("rightAnswers").EnumerateArray().Select(a => a.GetString())
            )
            .ToList();
    }
}
