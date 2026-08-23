using AutoMapper;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Courses.Models;
using Noo.Api.Courses.Services;
using Noo.Api.Media.Events;
using Noo.Api.Media.Models;
using Noo.Api.Media.Types;
using Noo.Api.MediaDownloads.Events;
using Noo.Api.MediaDownloads.Filters;
using Noo.Api.MediaDownloads.Services;
using Noo.Api.Subjects.Models;
using Noo.Api.Users.Models;
using Noo.Api.Users.Services;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.MediaDownloads;

public class MediaDownloadServiceTests
{
    private static IMapper CreateMapper() => MapperTestUtils.CreateAppMapper();

    private static MediaDownloadService MakeService(NooDbContext ctx)
    {
        return new MediaDownloadService(
            new MediaDownloadRepository(ctx),
            new CourseContentRepository(ctx),
            new UserRepository(ctx),
            CreateMapper()
        );
    }

    private static MediaDownloadedHandler MakeHandler(NooDbContext ctx)
    {
        return new MediaDownloadedHandler(MakeService(ctx), new CourseContentRepository(ctx));
    }

    private static MediaModel MakeMedia(Ulid ownerId, Ulid contentId, string name)
    {
        return new MediaModel
        {
            Path = $"course-attachment/{ownerId}/{name}",
            Name = name,
            ActualName = name,
            Extension = "pdf",
            Size = 1024,
            Category = MediaCategory.CourseAttachment,
            Status = MediaStatus.Completed,
            EntityId = contentId,
            OwnerId = ownerId,
        };
    }

    private record Seed(Ulid MaterialId, Ulid FirstMediaId, Ulid SecondMediaId, Ulid StudentId);

    private static async Task<Seed> SeedMaterialWithTwoFilesAsync(NooDbContext ctx)
    {
        var teacher = new UserModel
        {
            Username = "teacher",
            Name = "Teacher",
            Email = "teacher@example.com",
        };
        var student = new UserModel
        {
            Username = "student",
            Name = "Student",
            Email = "student@example.com",
        };
        ctx.GetDbSet<UserModel>().AddRange(teacher, student);

        var subject = new SubjectModel { Name = "Math", Color = "red" };
        ctx.GetDbSet<SubjectModel>().Add(subject);

        var course = new CourseModel { Name = "Course 1", SubjectId = subject.Id };
        ctx.GetDbSet<CourseModel>().Add(course);

        var chapter = new CourseChapterModel
        {
            Title = "Chapter",
            CourseId = course.Id,
            IsActive = true,
        };
        ctx.GetDbSet<CourseChapterModel>().Add(chapter);

        var content = new CourseMaterialContentModel();
        ctx.GetDbSet<CourseMaterialContentModel>().Add(content);

        var first = MakeMedia(teacher.Id, content.Id, "first.pdf");
        var second = MakeMedia(teacher.Id, content.Id, "second.pdf");
        ctx.GetDbSet<MediaModel>().AddRange(first, second);

        content.Medias = [first, second];

        var material = new CourseMaterialModel
        {
            Title = "Material",
            ChapterId = chapter.Id,
            ContentId = content.Id,
            IsActive = true,
        };
        ctx.GetDbSet<CourseMaterialModel>().Add(material);

        await ctx.SaveChangesAsync();

        return new Seed(material.Id, first.Id, second.Id, student.Id);
    }

    [Fact]
    public async Task Handler_Records_A_Course_Attachment_Download()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var seed = await SeedMaterialWithTwoFilesAsync(ctx);

        await MakeHandler(ctx)
            .HandleAsync(
                new MediaDownloadedEvent(
                    seed.FirstMediaId,
                    seed.StudentId,
                    MediaCategory.CourseAttachment
                )
            );
        await ctx.SaveChangesAsync();

        var summary = (await MakeService(ctx).GetMaterialSummaryAsync(seed.MaterialId)).ToList();

        var first = summary.Single(s => s.Media.Id == seed.FirstMediaId);
        Assert.Equal(1, first.TotalDownloads);
        Assert.Equal(1, first.UniqueUsers);
        Assert.NotNull(first.LastDownloadAt);
    }

    [Fact]
    public async Task Handler_Ignores_Media_Outside_Course_Attachments()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var seed = await SeedMaterialWithTwoFilesAsync(ctx);

        await MakeHandler(ctx)
            .HandleAsync(
                new MediaDownloadedEvent(
                    seed.FirstMediaId,
                    seed.StudentId,
                    MediaCategory.CourseRichText
                )
            );
        await ctx.SaveChangesAsync();

        var summary = (await MakeService(ctx).GetMaterialSummaryAsync(seed.MaterialId)).ToList();

        Assert.All(summary, s => Assert.Equal(0, s.TotalDownloads));
    }

    [Fact]
    public async Task Summary_Lists_Files_Nobody_Downloaded()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var seed = await SeedMaterialWithTwoFilesAsync(ctx);

        var service = MakeService(ctx);
        service.Record(seed.FirstMediaId, seed.StudentId, seed.MaterialId);
        await ctx.SaveChangesAsync();

        var summary = (await service.GetMaterialSummaryAsync(seed.MaterialId)).ToList();

        Assert.Equal(2, summary.Count);

        var untouched = summary.Single(s => s.Media.Id == seed.SecondMediaId);
        Assert.Equal(0, untouched.TotalDownloads);
        Assert.Equal(0, untouched.UniqueUsers);
        Assert.Null(untouched.LastDownloadAt);
    }

    [Fact]
    public async Task Summary_Counts_Repeat_Downloads_Once_Per_User()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var seed = await SeedMaterialWithTwoFilesAsync(ctx);

        var service = MakeService(ctx);
        service.Record(seed.FirstMediaId, seed.StudentId, seed.MaterialId);
        service.Record(seed.FirstMediaId, seed.StudentId, seed.MaterialId);
        await ctx.SaveChangesAsync();

        var summary = (await service.GetMaterialSummaryAsync(seed.MaterialId)).ToList();

        var first = summary.Single(s => s.Media.Id == seed.FirstMediaId);
        Assert.Equal(2, first.TotalDownloads);
        Assert.Equal(1, first.UniqueUsers);
    }

    [Fact]
    public async Task Summary_Throws_For_An_Unknown_Material()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        await SeedMaterialWithTwoFilesAsync(ctx);

        await Assert.ThrowsAsync<NotFoundException>(
            () => MakeService(ctx).GetMaterialSummaryAsync(Ulid.NewUlid())
        );
    }

    [Fact]
    public async Task Downloaders_Aggregate_Per_User_And_Carry_The_User()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var seed = await SeedMaterialWithTwoFilesAsync(ctx);

        var other = new UserModel
        {
            Username = "student2",
            Name = "Student Two",
            Email = "student2@example.com",
        };
        ctx.GetDbSet<UserModel>().Add(other);
        await ctx.SaveChangesAsync();

        var service = MakeService(ctx);
        service.Record(seed.FirstMediaId, seed.StudentId, seed.MaterialId);
        service.Record(seed.FirstMediaId, seed.StudentId, seed.MaterialId);
        service.Record(seed.FirstMediaId, other.Id, seed.MaterialId);
        await ctx.SaveChangesAsync();

        var result = await service.GetMaterialDownloadersAsync(
            seed.MaterialId,
            new MaterialFileDownloadsFilter { Page = 1, PerPage = 10 }
        );

        Assert.Equal(2, result.Total);

        var student = result.Items.Single(i => i.UserId == seed.StudentId);
        Assert.Equal(2, student.DownloadCount);
        Assert.Equal("Student", student.User?.Name);
        Assert.True(student.LastDownloadAt >= student.FirstDownloadAt);
    }

    [Fact]
    public async Task Downloaders_Can_Be_Narrowed_To_One_File()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var seed = await SeedMaterialWithTwoFilesAsync(ctx);

        var service = MakeService(ctx);
        service.Record(seed.FirstMediaId, seed.StudentId, seed.MaterialId);
        service.Record(seed.SecondMediaId, seed.StudentId, seed.MaterialId);
        await ctx.SaveChangesAsync();

        var everything = await service.GetMaterialDownloadersAsync(
            seed.MaterialId,
            new MaterialFileDownloadsFilter { Page = 1, PerPage = 10 }
        );
        Assert.Equal(2, everything.Items.Single().DownloadCount);

        var justSecond = await service.GetMaterialDownloadersAsync(
            seed.MaterialId,
            new MaterialFileDownloadsFilter
            {
                Page = 1,
                PerPage = 10,
                MediaId = seed.SecondMediaId,
            }
        );
        Assert.Equal(1, justSecond.Items.Single().DownloadCount);
    }
}
