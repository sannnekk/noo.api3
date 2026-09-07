using System.Text.Json.Nodes;
using AutoMapper;
using Noo.Api.Core.DataAbstraction.Cache;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.System.Collaboration;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Works.Collaboration;
using Noo.Api.Works.DTO;
using Noo.Api.Works.Models;
using Noo.Api.Works.Services;
using Noo.Api.Works.Types;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.Works;

public class WorkCollaborationHandlerTests
{
    private static IMapper CreateMapper()
    {
        var config = MapperTestUtils.CreateMapperConfig(cfg =>
        {
            cfg.AddProfile<WorkMapperProfile>();
            cfg.AddProfile<Noo.Api.Subjects.Models.SubjectMapperProfile>();
            cfg.AddProfile<Noo.Api.Courses.Models.CourseMapperProfile>();
            cfg.AddProfile<Noo.Api.NooTube.Models.NooTubeMapperProfile>();
            cfg.AddProfile<Noo.Api.Media.Models.MediaMapperProfile>();
            cfg.AddProfile<Noo.Api.Polls.Models.PollMapperProfile>();
            cfg.AddProfile<Noo.Api.Users.Models.UserMapperProfile>();
        });
        config.AssertConfigurationIsValid();

        return config.CreateMapper();
    }

    private sealed record Harness(
        WorkCollaborationHandler Handler,
        IWorkService Service,
        Func<Task> Commit,
        Ulid WorkId
    );

    private static async Task<Harness> CreateAsync(int taskCount = 2)
    {
        var context = TestHelpers.CreateInMemoryDb(Guid.NewGuid().ToString());
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var service = new WorkService(
            new WorkRepository(context),
            mapper,
            new JsonPatchUpdateService(mapper),
            new MemoryCacheRepository()
        );

        var workId = service.CreateWork(
            new CreateWorkDTO
            {
                Title = "Работа",
                Type = WorkType.Test,
                SubjectId = Ulid.NewUlid(),
                Tasks =
                [
                    .. Enumerable
                        .Range(0, taskCount)
                        .Select(index => new CreateWorkTaskDTO
                        {
                            Type = WorkTaskType.Word,
                            Order = index,
                            MaxScore = 5,
                            Content = RichTextFactory.Create($"q{index}"),
                        }),
                ],
            }
        );
        await uow.CommitAsync();

        return new Harness(
            new WorkCollaborationHandler(service, mapper),
            service,
            () => uow.CommitAsync(),
            workId
        );
    }

    private static CollaborationOp Replace(string path, JsonNode? value) =>
        new()
        {
            Op = "replace",
            Path = path,
            Value = value,
        };

    [Fact(DisplayName = "WorkCollaborationHandler: only paths the patch endpoint accepts are editable")]
    public void IsPathAllowed_MatchesThePatchDocument()
    {
        var handler = new WorkCollaborationHandler(null!, null!);
        var taskId = Ulid.NewUlid().ToString();

        Assert.True(handler.IsPathAllowed("/title"));
        Assert.True(handler.IsPathAllowed("/subjectId"));
        Assert.True(handler.IsPathAllowed($"/tasks/{taskId}"));
        Assert.True(handler.IsPathAllowed($"/tasks/{taskId}/maxScore"));
        Assert.True(handler.IsPathAllowed($"/tasks/{taskId}/content"));

        Assert.False(handler.IsPathAllowed("/maxScore"));
        Assert.False(handler.IsPathAllowed("/id"));
        Assert.False(handler.IsPathAllowed("/tasks"));
        Assert.False(handler.IsPathAllowed("/tasks/0/maxScore"));
        Assert.False(handler.IsPathAllowed($"/tasks/{taskId}/content/content/0/text"));
    }

    // The whole point of expressing the draft as patch operations: saving is the existing PATCH
    // pipeline, so the total score and the task numbering are settled by the same code as before.
    [Fact(DisplayName = "WorkCollaborationHandler: saving a draft goes through the work's own patch pipeline")]
    public async Task SaveAsync_AppliesThroughTheWorkService()
    {
        var harness = await CreateAsync();
        var work = await harness.Service.GetWorkAsync(harness.WorkId);
        var first = work!.Tasks!.Single(task => task.Order == 1);

        await harness.Handler.SaveAsync(
            harness.WorkId,
            [
                Replace("/title", JsonValue.Create("Новое название")),
                Replace($"/tasks/{first.Id}/maxScore", JsonValue.Create(9)),
            ]
        );
        await harness.Commit();

        var saved = await harness.Service.GetWorkAsync(harness.WorkId);

        Assert.Equal("Новое название", saved!.Title);
        Assert.Equal(9, saved.Tasks!.Single(task => task.Id == first.Id).MaxScore);

        // Recomputed rather than taken from the draft: 9 + the untouched task's 5.
        Assert.Equal(14, saved.MaxScore);
    }

    [Fact(DisplayName = "WorkCollaborationHandler: a task added to the draft is created on save")]
    public async Task SaveAsync_CreatesADraftedTask()
    {
        var harness = await CreateAsync();
        var newId = Ulid.NewUlid();

        await harness.Handler.SaveAsync(
            harness.WorkId,
            [
                new CollaborationOp
                {
                    Op = "add",
                    Path = $"/tasks/{newId}",
                    Value = JsonNode.Parse(
                        $$"""
                        {
                          "id": "{{newId}}", "type": "word", "order": 3, "maxScore": 4,
                          "content": {"$type":"tiptap","type":"doc","content":[
                            {"type":"paragraph","content":[{"type":"text","text":"новое"}]}]}
                        }
                        """
                    ),
                },
            ]
        );
        await harness.Commit();

        var saved = await harness.Service.GetWorkAsync(harness.WorkId);

        Assert.Equal(3, saved!.Tasks!.Count);
        Assert.Contains(saved.Tasks, task => task.Id == newId);
        Assert.Equal(14, saved.MaxScore);
    }

    // Compaction has to be an exact substitute for the operations it replaces, or a long session
    // would save something different from what everyone was looking at.
    [Fact(DisplayName = "WorkCollaborationHandler: a compacted draft saves the same work as the original")]
    public async Task CompactAsync_PreservesWhatTheDraftMeant()
    {
        var harness = await CreateAsync();
        var work = await harness.Service.GetWorkAsync(harness.WorkId);
        var first = work!.Tasks!.Single(task => task.Order == 1);
        var second = work.Tasks!.Single(task => task.Order == 2);

        // A session's worth of churn: the same field written repeatedly, and a task removed.
        List<CollaborationOp> ops =
        [
            Replace("/title", JsonValue.Create("Первое")),
            Replace("/title", JsonValue.Create("Второе")),
            Replace($"/tasks/{first.Id}/maxScore", JsonValue.Create(2)),
            Replace($"/tasks/{first.Id}/maxScore", JsonValue.Create(7)),
            new CollaborationOp { Op = "remove", Path = $"/tasks/{second.Id}" },
        ];

        var compacted = await harness.Handler.CompactAsync(harness.WorkId, ops);

        Assert.True(compacted.Count < ops.Count + 4);

        await harness.Handler.SaveAsync(harness.WorkId, compacted);
        await harness.Commit();

        var saved = await harness.Service.GetWorkAsync(harness.WorkId);

        Assert.Equal("Второе", saved!.Title);
        Assert.Equal(7, saved.Tasks!.Single().MaxScore);
        Assert.Equal(7, saved.MaxScore);
    }

    [Fact(DisplayName = "WorkCollaborationHandler: a work that does not exist cannot be opened")]
    public async Task CanEditAsync_RequiresTheWorkToExist()
    {
        var harness = await CreateAsync();

        Assert.True(await harness.Handler.CanEditAsync(harness.WorkId));
        Assert.False(await harness.Handler.CanEditAsync(Ulid.NewUlid()));
    }
}
