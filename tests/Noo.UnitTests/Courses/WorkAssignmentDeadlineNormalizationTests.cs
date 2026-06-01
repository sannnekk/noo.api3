using Noo.Api.Courses.DTO;
using Noo.Api.Courses.Models;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.Courses;

public class WorkAssignmentDeadlineNormalizationTests
{
    private static readonly DateTime PickedDate = new(2026, 6, 2, 0, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime EndOfDay = new(2026, 6, 2, 23, 59, 59, DateTimeKind.Unspecified);

    [Fact]
    public void Create_NormalizesDeadlinesToEndOfMoscowDay()
    {
        var mapper = MapperTestUtils.CreateAppMapper();
        var dto = new CreateCourseWorkAssignmentDTO
        {
            WorkId = Ulid.NewUlid(),
            IsActive = true,
            SolveDeadlineAt = PickedDate,
            CheckDeadlineAt = PickedDate
        };

        var model = mapper.Map<CourseWorkAssignmentModel>(dto);

        Assert.Equal(EndOfDay, model.SolveDeadlineAt);
        Assert.Equal(EndOfDay, model.CheckDeadlineAt);
    }

    [Fact]
    public void Update_NormalizesDeadlinesToEndOfMoscowDay()
    {
        var mapper = MapperTestUtils.CreateAppMapper();
        var model = new CourseWorkAssignmentModel { WorkId = Ulid.NewUlid(), IsActive = true };
        var dto = new UpdateCourseWorkAssignmentDTO
        {
            SolveDeadlineAt = PickedDate,
            CheckDeadlineAt = PickedDate
        };

        mapper.Map(dto, model);

        Assert.Equal(EndOfDay, model.SolveDeadlineAt);
        Assert.Equal(EndOfDay, model.CheckDeadlineAt);
    }

    [Fact]
    public void Create_LeavesNullDeadlineNull()
    {
        var mapper = MapperTestUtils.CreateAppMapper();
        var dto = new CreateCourseWorkAssignmentDTO
        {
            WorkId = Ulid.NewUlid(),
            IsActive = true,
            SolveDeadlineAt = null
        };

        var model = mapper.Map<CourseWorkAssignmentModel>(dto);

        Assert.Null(model.SolveDeadlineAt);
    }

    [Fact]
    public void AlreadyEndOfDay_IsIdempotent()
    {
        var mapper = MapperTestUtils.CreateAppMapper();
        var dto = new CreateCourseWorkAssignmentDTO
        {
            WorkId = Ulid.NewUlid(),
            IsActive = true,
            SolveDeadlineAt = EndOfDay
        };

        var model = mapper.Map<CourseWorkAssignmentModel>(dto);

        Assert.Equal(EndOfDay, model.SolveDeadlineAt);
    }
}
