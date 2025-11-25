using AutoMapper;
using Noo.Api.Courses.DTO;
using Noo.Api.Courses.Models;
using Noo.Api.Media.Models;
using Noo.Api.NooTube.Models;
using Noo.Api.Polls.Models;
using Noo.Api.Users.Models;

namespace Noo.UnitTests.Courses;

public class CourseMapperProfileTests
{
    private static MapperConfiguration CreateConfiguration()
        => new(cfg =>
        {
            cfg.AddProfile(new CourseMapperProfile());
            cfg.AddProfile(new NooTubeMapperProfile());
            cfg.AddProfile(new MediaMapperProfile());
            cfg.AddProfile(new PollMapperProfile());
            cfg.AddProfile(new UserMapperProfile());
        });

    [Fact]
    public void CourseProfile_Config_Valid()
    {
        var cfg = CreateConfiguration();
        cfg.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_CreateCourse_To_Model_Maps_Fields()
    {
        var mapper = CreateConfiguration().CreateMapper();
        var dto = new CreateCourseDTO
        {
            Name = "Test",
            SubjectId = Ulid.NewUlid(),
            Description = "desc"
        };
        var model = mapper.Map<CourseModel>(dto);
        Assert.Equal(dto.Name, model.Name);
        Assert.Equal(dto.SubjectId, model.SubjectId);
        Assert.Equal(dto.Description, model.Description);
    }

    [Fact]
    public void Map_CreateMembership_To_Model_Maps_Fields()
    {
        var mapper = CreateConfiguration().CreateMapper();
        var dto = new CreateCourseMembershipDTO
        {
            CourseId = Ulid.NewUlid(),
            StudentId = Ulid.NewUlid()
        };
        var model = mapper.Map<CourseMembershipModel>(dto);
        Assert.Equal(dto.CourseId, model.CourseId);
        Assert.Equal(dto.StudentId, model.StudentId);
    }
}
