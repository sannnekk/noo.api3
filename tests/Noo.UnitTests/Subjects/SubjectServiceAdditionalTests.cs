using AutoMapper;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Subjects.DTO;
using Noo.Api.Subjects.Filters;
using Noo.Api.Subjects.Models;
using Noo.Api.Subjects.Services;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.Subjects;

public class SubjectServiceAdditionalTests
{
    private static IMapper CreateMapper()
    {
        var config = MapperTestUtils.CreateMapperConfig(cfg => cfg.AddProfile<SubjectMapperProfile>());
        config.AssertConfigurationIsValid();
        return config.CreateMapper();
    }

    [Fact]
    public async Task GetSubjectById_Returns_Null_For_Missing()
    {
        var db = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(db);
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var repository = new SubjectRepository(context);
        var jsonPatchService = new JsonPatchUpdateService(mapper);
        var service = new SubjectService(repository, jsonPatchService, mapper);
        var result = await service.GetSubjectByIdAsync(Ulid.NewUlid());
        Assert.Null(result);
    }

    [Fact]
    public async Task Update_NonExistent_Subject_Should_Throw_NotFound()
    {
        var db = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(db);
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var repository = new SubjectRepository(context);
        var jsonPatchService = new JsonPatchUpdateService(mapper);
        var service = new SubjectService(repository, jsonPatchService, mapper);
        var patch = new SystemTextJsonPatch.JsonPatchDocument<SubjectUpdateDTO>();
        patch.Replace(x => x.Name, "New");
        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateSubjectAsync(Ulid.NewUlid(), patch));
    }

    [Fact]
    public async Task Update_Subject_Invalid_Data_Should_Throw_BadRequest()
    {
        var db = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(db);
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var repository = new SubjectRepository(context);
        var jsonPatchService = new JsonPatchUpdateService(mapper);
        var service = new SubjectService(repository, jsonPatchService, mapper);

        var id = service.CreateSubject(new SubjectCreationDTO { Name = "Math", Color = "#123456" });
        await uow.CommitAsync();

        var patch = new SystemTextJsonPatch.JsonPatchDocument<SubjectUpdateDTO>();
        patch.Replace(x => x.Color, "not-a-color"); // invalid regex
        await Assert.ThrowsAsync<BadRequestException>(() => service.UpdateSubjectAsync(id, patch));
    }

    [Fact]
    public void Delete_NonExistent_Subject_Does_Not_Throw()
    {
        var db = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(db);
        var mapper = CreateMapper();
        var repository = new SubjectRepository(context);
        var jsonPatchService = new JsonPatchUpdateService(mapper);
        var service = new SubjectService(repository, jsonPatchService, mapper);
        // Without a commit, deleting a non-existent entity is a no-op
        service.DeleteSubject(Ulid.NewUlid());
    }

    [Fact]
    public async Task GetSubjects_Pagination_Works()
    {
        var db = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(db);
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var repository = new SubjectRepository(context);
        var jsonPatchService = new JsonPatchUpdateService(mapper);
        var service = new SubjectService(repository, jsonPatchService, mapper);

        for (int i = 0; i < 15; i++)
        {
            service.CreateSubject(new SubjectCreationDTO { Name = $"Sub{i}", Color = "#123456" });
        }
        await uow.CommitAsync();

        var page1 = await service.GetSubjectsAsync(new SubjectFilter { Page = 1, PerPage = 10 });
        var page2 = await service.GetSubjectsAsync(new SubjectFilter { Page = 2, PerPage = 10 });

        Assert.Equal(15, page1.Total);
        Assert.Equal(10, page1.Items.Count());
        Assert.Equal(15, page2.Total);
        Assert.Equal(5, page2.Items.Count());
        // Ensure no overlap of IDs between pages
        var page1Ids = page1.Items.Select(i => i.Id).ToHashSet();
        Assert.True(page2.Items.All(i => !page1Ids.Contains(i.Id)));
    }

    [Fact]
    public async Task Update_With_Empty_Patch_No_Change()
    {
        var db = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(db);
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var repository = new SubjectRepository(context);
        var jsonPatchService = new JsonPatchUpdateService(mapper);
        var service = new SubjectService(repository, jsonPatchService, mapper);
        var id = service.CreateSubject(new SubjectCreationDTO { Name = "Physics", Color = "#abcdef" });
        await uow.CommitAsync();
        var before = await service.GetSubjectByIdAsync(id);
        var emptyPatch = new SystemTextJsonPatch.JsonPatchDocument<SubjectUpdateDTO>();
        await service.UpdateSubjectAsync(id, emptyPatch);
        await uow.CommitAsync();
        var after = await service.GetSubjectByIdAsync(id);
        Assert.Equal(before!.Name, after!.Name);
        Assert.Equal(before.Color, after.Color);
    }
}
