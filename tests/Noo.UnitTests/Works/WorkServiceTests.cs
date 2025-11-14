using AutoMapper;
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
        var config = new MapperConfiguration(cfg =>
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
        var service = new WorkService(uow, repository, mapper, patchUpdater);

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

        var id = await service.CreateWorkAsync(create);
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

        var updated = await service.GetWorkAsync(id);
        Assert.Equal("Updated Title", updated!.Title);

        // Delete in a fresh context to avoid tracking conflict
        using var deleteContext = TestHelpers.CreateInMemoryDb(dbName);
        var deleteUow = TestHelpers.CreateUowMock(deleteContext).Object;
        var deleteRepository = new WorkRepository(deleteContext);
        var deleteService = new WorkService(deleteUow, deleteRepository, mapper, patchUpdater);
        await deleteService.DeleteWorkAsync(id);
        using var verifyContext = TestHelpers.CreateInMemoryDb(dbName);
        var verifyUow = TestHelpers.CreateUowMock(verifyContext).Object;
        var verifyRepository = new WorkRepository(verifyContext);
        var verifyService = new WorkService(verifyUow, verifyRepository, mapper, patchUpdater);
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
        var service = new WorkService(uow, repository, mapper, new JsonPatchUpdateService(mapper));

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
        var service = new WorkService(uow, repository, mapper, patchService);

        // Seed a work
        var id = await service.CreateWorkAsync(new CreateWorkDTO
        {
            Title = "Work",
            Type = WorkType.Test,
            SubjectId = Ulid.NewUlid(),
            Tasks =
            [
                new CreateWorkTaskDTO { Type = WorkTaskType.Word, Order = 0, MaxScore = 1, Content = DeltaRichText.FromString("abc") }
            ]
        });

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
        var service = new WorkService(uow, repository, mapper, new JsonPatchUpdateService(mapper));

        // Should not throw
        await service.DeleteWorkAsync(Ulid.NewUlid());
    }
}
