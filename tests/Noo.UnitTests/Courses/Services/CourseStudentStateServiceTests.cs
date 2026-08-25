using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Courses.DTO;
using Noo.Api.Courses.Models;
using Noo.Api.Courses.Services;
using Noo.UnitTests.Common;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace Noo.UnitTests.Courses.Services;

public class CourseStudentStateServiceTests
{
    private static CourseStudentStateService CreateService(NooDbContext ctx)
    {
        return new CourseStudentStateService(
            ctx,
            new JsonPatchUpdateService(MapperTestUtils.CreateAppMapper())
        );
    }

    private static JsonPatchDocument<UpdateCourseStudentStateDTO> Patch(
        string path,
        object value
    )
    {
        return new JsonPatchDocument<UpdateCourseStudentStateDTO>(
            [new Operation<UpdateCourseStudentStateDTO>("replace", path, null, value)],
            new System.Text.Json.JsonSerializerOptions()
        );
    }

    [Fact]
    public async Task First_Patch_Creates_The_Row()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var service = CreateService(ctx);
        var courseId = Ulid.NewUlid();
        var studentId = Ulid.NewUlid();

        await service.PatchStateAsync(courseId, studentId, Patch("/isPinned", true));
        await ctx.SaveChangesAsync();

        var state = await ctx.GetDbSet<CourseStudentStateModel>().SingleAsync();
        Assert.True(state.IsPinned);
        Assert.False(state.IsArchived);
    }

    [Fact]
    public async Task A_Second_Patch_Updates_The_Same_Row_Rather_Than_Adding_Another()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var service = CreateService(ctx);
        var courseId = Ulid.NewUlid();
        var studentId = Ulid.NewUlid();

        await service.PatchStateAsync(courseId, studentId, Patch("/isPinned", true));
        await ctx.SaveChangesAsync();
        var first = await ctx.GetDbSet<CourseStudentStateModel>().SingleAsync();

        await service.PatchStateAsync(courseId, studentId, Patch("/isArchived", true));
        await ctx.SaveChangesAsync();

        // Instance identity, not just field values: InMemory would happily track a second instance
        // for the same (course, student) pair, where the unique index in MySQL throws.
        var second = Assert.Single(ctx.GetDbSet<CourseStudentStateModel>());
        Assert.Same(first, second);
        Assert.True(second.IsPinned);
        Assert.True(second.IsArchived);
    }

    [Fact]
    public async Task Two_Patches_Before_A_Save_Still_Touch_One_Row()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var service = CreateService(ctx);
        var courseId = Ulid.NewUlid();
        var studentId = Ulid.NewUlid();

        await service.PatchStateAsync(courseId, studentId, Patch("/isPinned", true));
        await service.PatchStateAsync(courseId, studentId, Patch("/isArchived", true));
        await ctx.SaveChangesAsync();

        var state = Assert.Single(ctx.GetDbSet<CourseStudentStateModel>());
        Assert.True(state.IsPinned);
        Assert.True(state.IsArchived);
    }

    [Fact]
    public async Task Different_Students_Get_Their_Own_Rows()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var service = CreateService(ctx);
        var courseId = Ulid.NewUlid();

        await service.PatchStateAsync(courseId, Ulid.NewUlid(), Patch("/isPinned", true));
        await service.PatchStateAsync(courseId, Ulid.NewUlid(), Patch("/isPinned", true));
        await ctx.SaveChangesAsync();

        Assert.Equal(2, await ctx.GetDbSet<CourseStudentStateModel>().CountAsync());
    }

    [Fact]
    public async Task A_Field_The_Patch_Omits_Is_Left_Alone()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var service = CreateService(ctx);
        var courseId = Ulid.NewUlid();
        var studentId = Ulid.NewUlid();

        await service.PatchStateAsync(courseId, studentId, Patch("/isPinned", true));
        await service.PatchStateAsync(courseId, studentId, Patch("/isArchived", true));
        await ctx.SaveChangesAsync();

        await service.PatchStateAsync(courseId, studentId, Patch("/isArchived", false));
        await ctx.SaveChangesAsync();

        var state = await ctx.GetDbSet<CourseStudentStateModel>().SingleAsync();
        Assert.True(state.IsPinned);
        Assert.False(state.IsArchived);
    }
}
