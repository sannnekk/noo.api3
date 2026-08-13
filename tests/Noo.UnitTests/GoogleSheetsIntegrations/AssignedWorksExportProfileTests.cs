using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.GoogleSheetsIntegrations.Exports;
using Noo.Api.GoogleSheetsIntegrations.Exports.Profiles;
using Noo.Api.Users.Models;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.GoogleSheetsIntegrations;

public class AssignedWorksExportProfileTests
{
    private static readonly Ulid _mentorId = Ulid.NewUlid();
    private static readonly Ulid _otherMentorId = Ulid.NewUlid();
    private static readonly Ulid _ownStudentId = Ulid.NewUlid();
    private static readonly Ulid _otherStudentId = Ulid.NewUlid();

    private static AssignedWorksExportProfile CreateProfile()
    {
        var db = TestHelpers.CreateInMemoryDb();

        db.GetDbSet<MentorAssignmentModel>()
            .Add(
                new MentorAssignmentModel { MentorId = _mentorId, StudentId = _ownStudentId }
            );

        db.SaveChanges();

        return new AssignedWorksExportProfile(db);
    }

    [Fact]
    public void Validate_Requires_Exactly_One_Of_Student_Or_Mentor()
    {
        var profile = CreateProfile();

        Assert.Throws<BadRequestException>(() => profile.Validate(new ExportParameters()));

        Assert.Throws<BadRequestException>(
            () =>
                profile.Validate(
                    new ExportParameters { StudentId = _ownStudentId, MentorId = _mentorId }
                )
        );

        profile.Validate(new ExportParameters { StudentId = _ownStudentId });
        profile.Validate(new ExportParameters { MentorId = _mentorId });
    }

    [Theory]
    [InlineData(nameof(UserRoles.Admin), true)]
    [InlineData(nameof(UserRoles.Teacher), true)]
    [InlineData(nameof(UserRoles.Assistant), false)]
    [InlineData(nameof(UserRoles.Student), false)]
    public async Task AuthorizeAsync_Gates_On_Role(string roleName, bool expected)
    {
        var profile = CreateProfile();
        var role = Enum.Parse<UserRoles>(roleName);

        var allowed = await profile.AuthorizeAsync(
            Ulid.NewUlid(),
            role,
            new ExportParameters { StudentId = _otherStudentId }
        );

        Assert.Equal(expected, allowed);
    }

    [Fact]
    public async Task Mentor_Can_Export_Their_Own_Student()
    {
        var profile = CreateProfile();

        Assert.True(
            await profile.AuthorizeAsync(
                _mentorId,
                UserRoles.Mentor,
                new ExportParameters { StudentId = _ownStudentId }
            )
        );
    }

    [Fact]
    public async Task Mentor_Cannot_Export_Another_Mentors_Student()
    {
        var profile = CreateProfile();

        Assert.False(
            await profile.AuthorizeAsync(
                _mentorId,
                UserRoles.Mentor,
                new ExportParameters { StudentId = _otherStudentId }
            )
        );
    }

    [Fact]
    public async Task Mentor_Can_Export_Their_Own_Workload()
    {
        var profile = CreateProfile();

        Assert.True(
            await profile.AuthorizeAsync(
                _mentorId,
                UserRoles.Mentor,
                new ExportParameters { MentorId = _mentorId }
            )
        );
    }

    [Fact]
    public async Task Mentor_Cannot_Export_Another_Mentors_Workload()
    {
        var profile = CreateProfile();

        Assert.False(
            await profile.AuthorizeAsync(
                _mentorId,
                UserRoles.Mentor,
                new ExportParameters { MentorId = _otherMentorId }
            )
        );
    }

    [Fact]
    public async Task Teacher_Can_Export_Any_Student()
    {
        var profile = CreateProfile();

        Assert.True(
            await profile.AuthorizeAsync(
                Ulid.NewUlid(),
                UserRoles.Teacher,
                new ExportParameters { StudentId = _otherStudentId }
            )
        );
    }

    [Fact]
    public async Task Mentor_Role_Column_Appears_Only_For_The_Mentor_Variant()
    {
        var profile = CreateProfile();

        var byMentor = await profile.BuildAsync(new ExportParameters { MentorId = _mentorId });
        var byStudent = await profile.BuildAsync(
            new ExportParameters { StudentId = _ownStudentId }
        );

        Assert.Contains("Роль куратора", byMentor.Headers);
        Assert.DoesNotContain("Роль куратора", byStudent.Headers);
    }

    [Fact]
    public async Task Headers_Cover_Every_Requested_Field()
    {
        var profile = CreateProfile();

        var data = await profile.BuildAsync(new ExportParameters { StudentId = _ownStudentId });

        Assert.Equal(
            [
                "Ученик",
                "Email",
                "Telegram",
                "Название работы",
                "Предмет",
                "Балл",
                "Макс. балл",
                "Процент",
                "Дедлайн сдачи",
                "Сдано",
                "Дедлайн проверки",
                "Проверено",
            ],
            data.Headers
        );
    }
}
