using AutoMapper;
using Noo.Api.Core.Utils.Richtext.Delta;
using Noo.Api.Courses.Models;
using Noo.Api.Media.Models;
using Noo.Api.NooTube.Models;
using Noo.Api.Polls.Models;
using Noo.Api.Subjects.Models;
using Noo.Api.Users.Models;
using Noo.Api.Works.DTO;
using Noo.Api.Works.Models;
using Noo.Api.Works.Types;
using Noo.UnitTests.Common;
using SystemTextJsonPatch;
using Noo.Api.Core.Request.Patching;

namespace Noo.UnitTests.Works;

public class WorkMapperProfileTests
{
    private readonly IMapper _mapper;

    public WorkMapperProfileTests()
    {
        var config = MapperTestUtils.CreateMapperConfig(cfg =>
        {
            cfg.AddProfile<WorkMapperProfile>();
            cfg.AddProfile<SubjectMapperProfile>();
            cfg.AddProfile<CourseMapperProfile>();
            cfg.AddProfile<NooTubeMapperProfile>();
            cfg.AddProfile<MediaMapperProfile>();
            cfg.AddProfile<PollMapperProfile>();
            cfg.AddProfile<UserMapperProfile>();
        });
        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact(DisplayName = "Mapper: CreateWorkDTO -> WorkModel maps correctly")]
    public void Map_CreateWorkDTO_To_WorkModel()
    {
        var dto = new CreateWorkDTO
        {
            Title = "Algebra Test",
            Type = WorkType.Test,
            Description = "desc",
            SubjectId = Ulid.NewUlid(),
            Tasks =
            [
                new CreateWorkTaskDTO { Type = WorkTaskType.Text, Order = 1, MaxScore = 5, Content = DeltaRichText.FromString("abc") }
            ]
        };

        var model = _mapper.Map<WorkModel>(dto);

        Assert.Equal(dto.Title, model.Title);
        Assert.Equal(dto.Type, model.Type);
        Assert.Equal(dto.Description, model.Description);
        Assert.Equal(dto.SubjectId, model.SubjectId);
        Assert.Null(model.Subject);
    }

    [Fact(DisplayName = "Mapper: WorkModel -> UpdateWorkDTO and back updates title only")]
    public void Map_WorkModel_To_UpdateWorkDTO_And_Back()
    {
        var model = new WorkModel
        {
            Id = Ulid.NewUlid(),
            Title = "Geometry",
            Type = WorkType.MiniTest,
            Description = "d",
            SubjectId = Ulid.NewUlid()
        };

        var dto = _mapper.Map<UpdateWorkDTO>(model);
        Assert.Equal(model.Title, dto.Title);
        Assert.Equal(model.Type, dto.Type);
        Assert.Equal(model.Description, dto.Description);
        Assert.Equal(model.SubjectId, dto.SubjectId);

        // change fields via DTO and map back, ignoring non-updatable fields as configured
        dto.Title = "Geometry Updated";
        var updated = _mapper.Map(dto, model);
        Assert.Equal("Geometry Updated", updated.Title);
        Assert.Equal(model.Type, updated.Type);
    }

    // ----------------------------------------------------------------------
    // Patch round-trip regression tests (Tasks).
    //
    // Replicates the production flow JsonPatchUpdateService runs: Model -> DTO
    // -> mutate -> Map(DTO, Model). After the round-trip, existing children
    // must keep their original Ids and content (and be the SAME instance, so
    // EF tracks the in-place edit), and only the patched diff is observable.
    //
    // Original bug: existing WorkTaskModel children were rebuilt as fresh
    // instances with new random Ids — EF saw them as inserts, the originals
    // as orphans, and cascade-corrupted assigned_work_answer rows via
    // task_id FK.
    // ----------------------------------------------------------------------

    [Fact(DisplayName = "Mapper: PATCH that adds a task preserves existing task identity and content")]
    public void Patch_Add_Task_Preserves_Existing_Tasks()
    {
        var existingTaskId = Ulid.NewUlid();
        var existingContent = DeltaRichText.FromString("original");

        var model = new WorkModel
        {
            Id = Ulid.NewUlid(),
            Title = "W",
            Type = WorkType.Test,
            SubjectId = Ulid.NewUlid(),
            Tasks = new List<WorkTaskModel>
            {
                new()
                {
                    Id = existingTaskId,
                    Order = 0,
                    Type = WorkTaskType.Word,
                    MaxScore = 5,
                    Content = existingContent,
                }
            }
        };
        var existingTaskRef = model.Tasks.First();

        // Simulate the patch service round-trip.
        var dto = _mapper.Map<UpdateWorkDTO>(model);

        // Add a new task entry, keyed by its new Id.
        var newTaskId = Ulid.NewUlid();
        dto.Tasks![newTaskId.ToString()] = new UpdateWorkTaskDTO
        {
            Id = newTaskId,
            Order = 1,
            Type = WorkTaskType.Word,
            MaxScore = 3,
            Content = DeltaRichText.FromString("brand new task"),
        };

        _mapper.Map(dto, model);

        Assert.NotNull(model.Tasks);
        Assert.Equal(2, model.Tasks!.Count);

        // The existing task must be the SAME instance (EF tracking preserved)
        // with its original Id and content intact.
        var keptTask = model.Tasks.Single(t => t.Id == existingTaskId);
        Assert.Same(existingTaskRef, keptTask);
        Assert.Same(existingContent, keptTask.Content);
        Assert.Equal(5, keptTask.MaxScore);

        // The new task is present with the Id from the dict key.
        Assert.Contains(model.Tasks, t => t.Id == newTaskId);
    }

    [Fact(DisplayName = "Mapper: PATCH that updates a task changes only its fields, not its identity")]
    public void Patch_Update_Task_Preserves_Identity()
    {
        var taskId = Ulid.NewUlid();
        var model = new WorkModel
        {
            Id = Ulid.NewUlid(),
            Title = "W",
            Type = WorkType.Test,
            SubjectId = Ulid.NewUlid(),
            Tasks = new List<WorkTaskModel>
            {
                new()
                {
                    Id = taskId,
                    Order = 0,
                    Type = WorkTaskType.Word,
                    MaxScore = 5,
                    Content = DeltaRichText.FromString("v1"),
                }
            }
        };
        var existingTaskRef = model.Tasks.First();

        var dto = _mapper.Map<UpdateWorkDTO>(model);
        dto.Tasks![taskId.ToString()].MaxScore = 99;
        dto.Tasks![taskId.ToString()].Content = DeltaRichText.FromString("v2");

        _mapper.Map(dto, model);

        Assert.Single(model.Tasks!);
        var updated = model.Tasks!.Single();
        Assert.Same(existingTaskRef, updated);
        Assert.Equal(taskId, updated.Id);
        Assert.Equal(99, updated.MaxScore);
    }

    [Fact(DisplayName = "Mapper: PATCH that removes a task drops it from the collection")]
    public void Patch_Remove_Task_Drops_It()
    {
        var keepId = Ulid.NewUlid();
        var dropId = Ulid.NewUlid();

        var model = new WorkModel
        {
            Id = Ulid.NewUlid(),
            Title = "W",
            Type = WorkType.Test,
            SubjectId = Ulid.NewUlid(),
            Tasks = new List<WorkTaskModel>
            {
                new() { Id = keepId, Order = 0, Type = WorkTaskType.Word, MaxScore = 1, Content = DeltaRichText.FromString("k") },
                new() { Id = dropId, Order = 1, Type = WorkTaskType.Word, MaxScore = 1, Content = DeltaRichText.FromString("d") },
            }
        };

        var dto = _mapper.Map<UpdateWorkDTO>(model);
        dto.Tasks!.Remove(dropId.ToString());

        _mapper.Map(dto, model);

        Assert.Single(model.Tasks!);
        Assert.Equal(keepId, model.Tasks!.Single().Id);
    }

    [Fact(DisplayName = "Mapper: PATCH via JsonPatchUpdateService — adding a task preserves existing rows")]
    public void Patch_End_To_End_Add_Task_Preserves_Existing_Rows()
    {
        // This is the closest possible reproduction of the original bug at the
        // mapper boundary: drive the same code path JsonPatchUpdateService uses
        // (Model -> DTO -> JSON patch ops -> Map(DTO, Model)).
        var patchService = new JsonPatchUpdateService(_mapper);

        var existingTaskId = Ulid.NewUlid();
        var existingContent = DeltaRichText.FromString("original content");
        var model = new WorkModel
        {
            Id = Ulid.NewUlid(),
            Title = "W",
            Type = WorkType.Test,
            SubjectId = Ulid.NewUlid(),
            Tasks = new List<WorkTaskModel>
            {
                new()
                {
                    Id = existingTaskId,
                    Order = 0,
                    Type = WorkTaskType.Word,
                    MaxScore = 5,
                    Content = existingContent,
                }
            }
        };
        var existingTaskRef = model.Tasks.First();

        var newTaskId = Ulid.NewUlid();
        var patch = new JsonPatchDocument<UpdateWorkDTO>();
        patch.Add(
            x => x.Tasks![newTaskId.ToString()],
            new UpdateWorkTaskDTO
            {
                Id = newTaskId,
                Order = 1,
                Type = WorkTaskType.Word,
                MaxScore = 3,
                Content = DeltaRichText.FromString("brand new"),
            });

        patchService.ApplyPatch(model, patch);

        Assert.Equal(2, model.Tasks!.Count);
        var keptTask = model.Tasks!.Single(t => t.Id == existingTaskId);
        Assert.Same(existingTaskRef, keptTask);
        Assert.Same(existingContent, keptTask.Content);
        Assert.Equal(5, keptTask.MaxScore);
    }
}
