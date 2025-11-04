using AutoMapper;
using Noo.Api.Courses.DTO;
using Noo.Api.Courses.Models;

namespace Noo.UnitTests.Courses;

public class CourseMapperProfileTests
{
    [Fact]
    public void CourseProfile_Config_Valid()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile(new CourseMapperProfile()));
        cfg.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_CreateCourse_To_Model_Maps_Fields()
    {
        var mapper = new MapperConfiguration(c => c.AddProfile(new CourseMapperProfile())).CreateMapper();
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
        var mapper = new MapperConfiguration(c => c.AddProfile(new CourseMapperProfile())).CreateMapper();
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
