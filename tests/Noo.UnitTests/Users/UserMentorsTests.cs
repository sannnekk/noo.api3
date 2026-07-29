using System.Text.Json;
using AutoMapper;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Subjects.Models;
using Noo.Api.Users.DTO;
using Noo.Api.Users.Filters;
using Noo.Api.Users.Models;
using Noo.Api.Users.Services;
using Noo.Api.Users.Specifications;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.Users;

/// <summary>
/// The user search lists the mentors of every student.
/// </summary>
public class UserMentorsTests
{
    private static IMapper CreateMapper() => MapperTestUtils.CreateAppMapper();

    private static UserModel MakeUser(string name, UserRoles role)
    {
        return new UserModel
        {
            Name = name,
            Username = name.ToLowerInvariant(),
            Email = $"{name.ToLowerInvariant()}@noo.test",
            Role = role,
            PasswordHash = "hash",
        };
    }

    private static async Task<UserModel> SeedStudentWithMentorAsync(
        NooDbContext context,
        bool withSubject = true
    )
    {
        var student = MakeUser("Student", UserRoles.Student);
        var mentor = MakeUser("Mentor", UserRoles.Mentor);
        var subject = new SubjectModel { Name = "Math", Color = "red" };

        context.GetDbSet<UserModel>().AddRange(student, mentor);
        context.GetDbSet<SubjectModel>().Add(subject);
        context
            .GetDbSet<MentorAssignmentModel>()
            .Add(
                new MentorAssignmentModel
                {
                    StudentId = student.Id,
                    MentorId = mentor.Id,
                    SubjectId = withSubject ? subject.Id : null,
                }
            );

        await context.SaveChangesAsync();

        return student;
    }

    private static Task<SearchResult<UserModel>> SearchAsync(NooDbContext context)
    {
        return new UserRepository(context).SearchAsync(
            new UserFilter { Page = 1, PerPage = 25 },
            [new UserWithAvatarSpecification()]
        );
    }

    [Fact]
    public async Task Search_Returns_Mentor_And_Subject_Of_A_Student()
    {
        using var context = TestHelpers.CreateInMemoryDb();
        var student = await SeedStudentWithMentorAsync(context);

        var result = await SearchAsync(context);
        var dto = CreateMapper()
            .Map<UserDTO>(result.Items.Single(user => user.Id == student.Id));

        var mentor = Assert.Single(dto.Mentors);
        Assert.Equal("Mentor", mentor.Name);
        Assert.Equal("Math", mentor.SubjectName);
        Assert.Equal("red", mentor.SubjectColor);
        Assert.NotEqual(student.Id, mentor.Id);
    }

    [Fact]
    public async Task Mentor_Id_Points_At_The_Mentor()
    {
        using var context = TestHelpers.CreateInMemoryDb();
        var student = await SeedStudentWithMentorAsync(context);

        var result = await SearchAsync(context);
        var mentorModel = result.Items.Single(user => user.Role == UserRoles.Mentor);
        var dto = CreateMapper()
            .Map<UserDTO>(result.Items.Single(user => user.Id == student.Id));

        Assert.Equal(mentorModel.Id, Assert.Single(dto.Mentors).Id);
    }

    [Fact]
    public async Task Mentors_Without_A_Subject_Keep_The_Mentor()
    {
        using var context = TestHelpers.CreateInMemoryDb();
        var student = await SeedStudentWithMentorAsync(context, withSubject: false);

        var result = await SearchAsync(context);
        var dto = CreateMapper()
            .Map<UserDTO>(result.Items.Single(user => user.Id == student.Id));

        var mentor = Assert.Single(dto.Mentors);
        Assert.Equal("Mentor", mentor.Name);
        Assert.Null(mentor.SubjectName);
        Assert.Null(mentor.SubjectColor);
    }

    [Fact]
    public async Task Users_Without_Mentors_Get_An_Empty_List()
    {
        using var context = TestHelpers.CreateInMemoryDb();
        await SeedStudentWithMentorAsync(context);

        var result = await SearchAsync(context);
        var dto = CreateMapper()
            .Map<UserDTO>(result.Items.Single(user => user.Role == UserRoles.Mentor));

        Assert.Empty(dto.Mentors);
    }

    [Fact]
    public async Task Mentors_Do_Not_Carry_The_Student_Back()
    {
        using var context = TestHelpers.CreateInMemoryDb();
        await SeedStudentWithMentorAsync(context);

        var result = await SearchAsync(context);
        var dtos = CreateMapper().Map<IEnumerable<UserDTO>>(result.Items);

        // EF fixes the student up onto its own assignments. The flat mentor
        // shape is what keeps that from turning the response into a cycle.
        var json = JsonSerializer.Serialize(dtos);

        Assert.Contains("\"subjectColor\":\"red\"", json);
    }
}
