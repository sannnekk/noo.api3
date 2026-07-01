using AutoFilterer.Extensions;
using Noo.Api.AssignedWorks.Filters;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Courses.Filters;
using Noo.Api.Courses.Models;
using Noo.Api.Users.Filters;
using Noo.Api.Users.Models;
using Noo.Api.Works.Filters;
using Noo.Api.Works.Models;
using Noo.Api.Works.Types;

namespace Noo.UnitTests.Core;

// Guards the [ArraySearchFilter] multi-value ("IN") filters. The nullable-value-type
// cases (Ulid? entity property) are the ones that silently returned everything when the
// filter collection element type did not match the entity property type.
public class ArraySearchFilterTests
{
    [Fact]
    public void WorkFilter_SubjectId_NullableUlid_FiltersIn()
    {
        var s1 = Ulid.NewUlid();
        var s2 = Ulid.NewUlid();
        var s3 = Ulid.NewUlid();

        var data = new[]
        {
            new WorkModel { Title = "a", Type = WorkType.Test, SubjectId = s1 },
            new WorkModel { Title = "b", Type = WorkType.Test, SubjectId = s2 },
            new WorkModel { Title = "c", Type = WorkType.Test, SubjectId = s3 },
        }.AsQueryable();

        var filter = new WorkFilter { SubjectId = new Ulid?[] { s1, s3 } };

        var result = data.ApplyFilter(filter).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, w => Assert.Contains(w.SubjectId, new Ulid?[] { s1, s3 }));
    }

    [Fact]
    public void WorkFilter_Type_Enum_FiltersIn()
    {
        var data = new[]
        {
            new WorkModel { Title = "a", Type = WorkType.Test },
            new WorkModel { Title = "b", Type = WorkType.Phrase },
            new WorkModel { Title = "c", Type = WorkType.MiniTest },
        }.AsQueryable();

        var filter = new WorkFilter { Type = new[] { WorkType.Test, WorkType.MiniTest } };

        var result = data.ApplyFilter(filter).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, w => Assert.True(w.Type is WorkType.Test or WorkType.MiniTest));
    }

    [Fact]
    public void CourseFilter_SubjectId_NullableUlid_FiltersIn()
    {
        var s1 = Ulid.NewUlid();
        var s2 = Ulid.NewUlid();
        var s3 = Ulid.NewUlid();

        var data = new[]
        {
            new CourseModel { Name = "a", SubjectId = s1 },
            new CourseModel { Name = "b", SubjectId = s2 },
            new CourseModel { Name = "c", SubjectId = s3 },
        }.AsQueryable();

        var filter = new CourseFilter { SubjectId = new Ulid?[] { s1, s3 } };

        var result = data.ApplyFilter(filter).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, c => Assert.Contains(c.SubjectId, new Ulid?[] { s1, s3 }));
    }

    [Fact]
    public void AssignedWorkFilter_Type_Enum_FiltersIn()
    {
        var data = new[]
        {
            new AssignedWorkModel { Title = "a", Type = WorkType.Test },
            new AssignedWorkModel { Title = "b", Type = WorkType.Phrase },
            new AssignedWorkModel { Title = "c", Type = WorkType.MiniTest },
        }.AsQueryable();

        var filter = new AssignedWorkFilter { Type = new[] { WorkType.Test, WorkType.MiniTest } };

        var result = data.ApplyFilter(filter).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, w => Assert.True(w.Type is WorkType.Test or WorkType.MiniTest));
    }

    [Fact]
    public void UserFilter_Role_Enum_FiltersIn()
    {
        var data = new[]
        {
            new UserModel { Name = "a", Username = "a", Email = "a", PasswordHash = "x", Role = UserRoles.Teacher },
            new UserModel { Name = "b", Username = "b", Email = "b", PasswordHash = "x", Role = UserRoles.Student },
            new UserModel { Name = "c", Username = "c", Email = "c", PasswordHash = "x", Role = UserRoles.Mentor },
        }.AsQueryable();

        var filter = new UserFilter { Role = new[] { UserRoles.Teacher, UserRoles.Mentor } };

        var result = data.ApplyFilter(filter).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, u => Assert.True(u.Role is UserRoles.Teacher or UserRoles.Mentor));
    }

    [Fact]
    public void ArraySearchFilter_SingleValue_StillFilters()
    {
        var s1 = Ulid.NewUlid();
        var s2 = Ulid.NewUlid();

        var data = new[]
        {
            new WorkModel { Title = "a", Type = WorkType.Test, SubjectId = s1 },
            new WorkModel { Title = "b", Type = WorkType.Test, SubjectId = s2 },
        }.AsQueryable();

        var filter = new WorkFilter { SubjectId = new Ulid?[] { s1 } };

        var result = data.ApplyFilter(filter).ToList();

        Assert.Single(result);
        Assert.Equal(s1, result[0].SubjectId);
    }
}
