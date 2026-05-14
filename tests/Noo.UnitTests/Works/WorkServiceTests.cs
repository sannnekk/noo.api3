using AutoMapper;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Utils.Richtext.Delta;
using Noo.Api.Works.DTO;
using Noo.Api.Works.Filters;
using Noo.Api.Works.Models;
using Noo.Api.Works.Services;
using Noo.Api.Works.Types;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.Works;

public class WorkServiceTests
{

    private static IMapper CreateMapper()
    {
        var config = MapperTestUtils.CreateMapperConfig(cfg =>
        {
            cfg.AddProfile<WorkMapperProfile>();
            cfg.AddProfile<Noo.Api.Subjects.Models.SubjectMapperProfile>();
        });
        config.AssertConfigurationIsValid();
        return config.CreateMapper();
    }

    [Fact(DisplayName = "WorkService: create, get, update, delete flow works")]
    public async Task CreateGetUpdateDelete_Work_Flow()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(dbName);
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var patchUpdater = new JsonPatchUpdateService(mapper);
        var repository = new WorkRepository(context);
        var service = new WorkService(repository, mapper, patchUpdater);

        // Create
        var create = new CreateWorkDTO
        {
            Title = "Test Work",
            Type = WorkType.Test,
            Description = "desc",
            SubjectId = Ulid.NewUlid(),
            Tasks =
            [
                new CreateWorkTaskDTO { Type = WorkTaskType.Word, Order = 0, MaxScore = 1, Content = DeltaRichText.FromString("abc") }
            ]
        };

        var id = service.CreateWork(create);
        await uow.CommitAsync();
        Assert.NotEqual(default, id);

        // Get
        var fetched = await service.GetWorkAsync(id);
        Assert.NotNull(fetched);
        Assert.Equal("Test Work", fetched!.Title);
        Assert.Single(fetched.Tasks!);

        // Search
        var search = await service.GetWorksAsync(new WorkFilter { Page = 1, PerPage = 10 });
        Assert.Equal(1, search.Total);
        Assert.Single(search.Items);

        // Update (patch title)
        var patch = new SystemTextJsonPatch.JsonPatchDocument<UpdateWorkDTO>();
        patch.Replace(x => x.Title, "Updated Title");
        await service.UpdateWorkAsync(id, patch);
        await uow.CommitAsync();

        var updated = await service.GetWorkAsync(id);
        Assert.Equal("Updated Title", updated!.Title);

        // Delete in a fresh context to avoid tracking conflict
        using var deleteContext = TestHelpers.CreateInMemoryDb(dbName);
        var deleteUow = TestHelpers.CreateUowMock(deleteContext).Object;
        var deleteRepository = new WorkRepository(deleteContext);
        var deleteService = new WorkService(deleteRepository, mapper, patchUpdater);
        deleteService.DeleteWork(id);
        await deleteUow.CommitAsync();
        using var verifyContext = TestHelpers.CreateInMemoryDb(dbName);
        var verifyUow = TestHelpers.CreateUowMock(verifyContext).Object;
        var verifyRepository = new WorkRepository(verifyContext);
        var verifyService = new WorkService(verifyRepository, mapper, patchUpdater);
        var afterDelete = await verifyService.GetWorkAsync(id);
        Assert.Null(afterDelete);
    }

    [Fact(DisplayName = "WorkService: Update non-existent work throws NotFound")]
    public async Task UpdateWork_NotFound_Throws()
    {
        using var context = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var repository = new WorkRepository(context);
        var service = new WorkService(repository, mapper, new JsonPatchUpdateService(mapper));

        var patch = new SystemTextJsonPatch.JsonPatchDocument<UpdateWorkDTO>();
        patch.Replace(x => x.Title, "Doesn't matter");

        await Assert.ThrowsAsync<Noo.Api.Core.Exceptions.Http.NotFoundException>(
            () => service.UpdateWorkAsync(Ulid.NewUlid(), patch));
    }

    [Fact(DisplayName = "WorkService: Invalid patch produces BadRequest")]
    public async Task UpdateWork_InvalidPatch_BadRequest()
    {
        using var context = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var patchService = new JsonPatchUpdateService(mapper);
        var repository = new WorkRepository(context);
        var service = new WorkService(repository, mapper, patchService);

        // Seed a work
        var id = service.CreateWork(new CreateWorkDTO
        {
            Title = "Work",
            Type = WorkType.Test,
            SubjectId = Ulid.NewUlid(),
            Tasks =
            [
                new CreateWorkTaskDTO { Type = WorkTaskType.Word, Order = 0, MaxScore = 1, Content = DeltaRichText.FromString("abc") }
            ]
        });
        await uow.CommitAsync();

        // Invalid because title length 0 (MinLength(1))
        var patch = new SystemTextJsonPatch.JsonPatchDocument<UpdateWorkDTO>();
        patch.Replace(x => x.Title, "");

        await Assert.ThrowsAsync<Noo.Api.Core.Exceptions.Http.BadRequestException>(
            () => service.UpdateWorkAsync(id, patch));
    }

    [Fact(DisplayName = "WorkService: Delete non-existent succeeds silently")]
    public async Task DeleteWork_NonExistent_NoThrow()
    {
        using var context = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var repository = new WorkRepository(context);
        var service = new WorkService(repository, mapper, new JsonPatchUpdateService(mapper));

        // Should not throw
        service.DeleteWork(Ulid.NewUlid());
    }

    [Fact(DisplayName = "Regression: PATCH /work adding a task must not corrupt linked AssignedWork answers")]
    public async Task Patch_Work_Add_Task_Does_Not_Null_AssignedWork_Answer_Content()
    {
        // Reproduces the original production bug: adding a task to a Work via JSON Patch
        // used to rebuild Work.Tasks as brand-new entities with brand-new Ids. EF then
        // saw the originals as orphans and the new ones as inserts; through the
        // task_id → work_task FK with cascade, that corrupted/nulled assigned_work_answer
        // rows linked to the original tasks.
        //
        // After the fix in NestedEntityMappingExtensions.MapDictionaryToCollection,
        // existing tasks retain their identity, so the answer rows must remain untouched.
        using var context = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var patchUpdater = new JsonPatchUpdateService(mapper);
        var service = new WorkService(new WorkRepository(context), mapper, patchUpdater);

        // 1. Seed a Work with one task.
        var workId = service.CreateWork(new CreateWorkDTO
        {
            Title = "Algebra",
            Type = WorkType.Test,
            SubjectId = Ulid.NewUlid(),
            Tasks =
            [
                new CreateWorkTaskDTO { Type = WorkTaskType.Word, Order = 0, MaxScore = 5, Content = DeltaRichText.FromString("Original task content") }
            ]
        });
        await uow.CommitAsync();
        var work = await service.GetWorkAsync(workId);
        var originalTask = work!.Tasks!.Single();
        var originalTaskId = originalTask.Id;

        // 2. Seed an AssignedWork + an answer referencing the original task, with content.
        var assignedWork = new AssignedWorkModel
        {
            Title = "Algebra AW",
            Type = WorkType.Test,
            StudentId = Ulid.NewUlid(),
            MaxScore = 5,
            WorkId = workId,
        };
        context.GetDbSet<AssignedWorkModel>().Add(assignedWork);
        var richTextContent = DeltaRichText.FromString("My answer text");
        var answer = new AssignedWorkAnswerModel
        {
            AssignedWorkId = assignedWork.Id,
            TaskId = originalTaskId,
            MaxScore = 5,
            Score = 4,
            Status = AssignedWorkAnswerStatus.Submitted,
            RichTextContent = richTextContent,
            WordContent = "word-answer",
        };
        context.GetDbSet<AssignedWorkAnswerModel>().Add(answer);
        await context.SaveChangesAsync();
        var originalAnswerId = answer.Id;

        // 3. PATCH the Work to add a new task (the operation that used to break things).
        var newTaskId = Ulid.NewUlid();
        var patch = new SystemTextJsonPatch.JsonPatchDocument<UpdateWorkDTO>();
        patch.Add(
            x => x.Tasks![newTaskId.ToString()],
            new UpdateWorkTaskDTO
            {
                Id = newTaskId,
                Type = WorkTaskType.Word,
                Order = 1,
                MaxScore = 3,
                Content = DeltaRichText.FromString("New task content"),
            });
        await service.UpdateWorkAsync(workId, patch);
        await uow.CommitAsync();

        // 4. Original task must still exist with the SAME Id.
        var workAfter = await service.GetWorkAsync(workId);
        Assert.Equal(2, workAfter!.Tasks!.Count);
        Assert.Contains(workAfter.Tasks!, t => t.Id == originalTaskId);
        Assert.Contains(workAfter.Tasks!, t => t.Id == newTaskId);

        // 5. THE ACTUAL REGRESSION ASSERTION: the answer row must still exist and its
        //    content must be intact — not nulled, not deleted, not re-keyed.
        var answerAfter = await context.GetDbSet<AssignedWorkAnswerModel>().FindAsync(originalAnswerId);
        Assert.NotNull(answerAfter);
        Assert.Equal(originalTaskId, answerAfter!.TaskId);
        Assert.Equal(assignedWork.Id, answerAfter.AssignedWorkId);
        Assert.NotNull(answerAfter.RichTextContent);
        Assert.Equal("word-answer", answerAfter.WordContent);
        Assert.Equal(AssignedWorkAnswerStatus.Submitted, answerAfter.Status);
        Assert.Equal(4, answerAfter.Score);
    }
}
