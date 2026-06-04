using Moq;
using Noo.Api.AssignedWorks.Events;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils;
using Noo.Api.Notifications.DTO;
using Noo.Api.Notifications.Services;
using Noo.Api.Users.Models;
using Noo.Api.Works.Types;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.AssignedWorks;

public class AssignedWorkSolvedHistoryHandlerTests
{
    [Fact]
    public async Task Adds_Solved_History_Entry()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var historyRepo = new AssignedWorkHistoryRepository(ctx);
        var handler = new SolvedHistoryHandler(historyRepo);

        var assignedWorkId = Ulid.NewUlid();
        var studentId = Ulid.NewUlid();

        await handler.HandleAsync(
            new SolvedEvent(assignedWorkId, studentId),
            CancellationToken.None
        );
        await ctx.SaveChangesAsync();

        var history = (await historyRepo.GetHistoryAsync(assignedWorkId)).ToList();
        var entry = Assert.Single(history);
        Assert.Equal(AssignedWorkHistoryType.Solved, entry.Type);
        Assert.Equal(studentId, entry.ChangedById);
    }
}

public class AssignedWorkSolvedNotificationHandlerTests
{
    private static (
        SolvedNotificationHandler handler,
        NooDbContext ctx,
        Mock<INotificationService> notifications,
        Mock<IAssignedWorkLinkGenerator> linkGen
    ) Create()
    {
        var ctx = TestHelpers.CreateInMemoryDb();
        var notifications = new Mock<INotificationService>();
        var linkGen = new Mock<IAssignedWorkLinkGenerator>();
        linkGen
            .Setup(l => l.GenerateViewLink(It.IsAny<Ulid>()))
            .Returns(new FrontendLink { Name = "link" });
        var repo = new AssignedWorkRepository(ctx);
        var handler = new SolvedNotificationHandler(notifications.Object, repo, linkGen.Object);
        return (handler, ctx, notifications, linkGen);
    }

    private static AssignedWorkModel Seed(NooDbContext ctx, bool withHelper)
    {
        var student = new UserModel
        {
            Name = "S",
            Username = $"stu_{Guid.NewGuid():N}",
            Email = $"{Guid.NewGuid():N}@e.com",
            PasswordHash = "p",
            Role = UserRoles.Student,
        };
        var mentor = new UserModel
        {
            Name = "M",
            Username = $"men_{Guid.NewGuid():N}",
            Email = $"{Guid.NewGuid():N}@e.com",
            PasswordHash = "p",
            Role = UserRoles.Mentor,
        };
        var helper = new UserModel
        {
            Name = "H",
            Username = $"hel_{Guid.NewGuid():N}",
            Email = $"{Guid.NewGuid():N}@e.com",
            PasswordHash = "p",
            Role = UserRoles.Mentor,
        };
        ctx.GetDbSet<UserModel>().AddRange(student, mentor, helper);

        var aw = new AssignedWorkModel
        {
            Title = "AW",
            Type = WorkType.Test,
            Attempt = 1,
            StudentId = student.Id,
            MainMentorId = mentor.Id,
            HelperMentorId = withHelper ? helper.Id : null,
            MaxScore = 10,
            SolveDeadlineAt = DateTime.UtcNow.AddDays(1),
            CheckDeadlineAt = DateTime.UtcNow.AddDays(2),
        };
        ctx.GetDbSet<AssignedWorkModel>().Add(aw);
        ctx.SaveChanges();
        return aw;
    }

    [Fact]
    public async Task Notifies_Student_And_Both_Mentors_When_Helper_Present()
    {
        var (handler, ctx, notifications, _) = Create();
        var aw = Seed(ctx, withHelper: true);

        BulkCreateNotificationsDTO? captured = null;
        notifications
            .Setup(n => n.BulkCreateNotificationsAsync(It.IsAny<BulkCreateNotificationsDTO>()))
            .Callback<BulkCreateNotificationsDTO>(dto => captured = dto)
            .Returns(Task.CompletedTask);

        await handler.HandleAsync(new SolvedEvent(aw.Id, aw.StudentId), CancellationToken.None);

        Assert.NotNull(captured);
        var recipients = new HashSet<Ulid>(captured!.UserIds);
        Assert.Equal(3, recipients.Count);
        Assert.Contains(aw.StudentId, recipients);
        Assert.Contains(aw.MainMentorId!.Value, recipients);
        Assert.Contains(aw.HelperMentorId!.Value, recipients);
        Assert.Equal("assigned_work.solved", captured.Type);
    }

    [Fact]
    public async Task Notifies_Student_And_Main_Mentor_When_No_Helper()
    {
        var (handler, ctx, notifications, _) = Create();
        var aw = Seed(ctx, withHelper: false);

        BulkCreateNotificationsDTO? captured = null;
        notifications
            .Setup(n => n.BulkCreateNotificationsAsync(It.IsAny<BulkCreateNotificationsDTO>()))
            .Callback<BulkCreateNotificationsDTO>(dto => captured = dto)
            .Returns(Task.CompletedTask);

        await handler.HandleAsync(new SolvedEvent(aw.Id, aw.StudentId), CancellationToken.None);

        Assert.NotNull(captured);
        var recipients = new HashSet<Ulid>(captured!.UserIds);
        Assert.Equal(2, recipients.Count);
        Assert.Contains(aw.StudentId, recipients);
        Assert.Contains(aw.MainMentorId!.Value, recipients);
    }

    [Fact]
    public async Task Does_Nothing_When_AssignedWork_Missing()
    {
        var (handler, _, notifications, _) = Create();

        await handler.HandleAsync(
            new SolvedEvent(Ulid.NewUlid(), Ulid.NewUlid()),
            CancellationToken.None
        );

        notifications.Verify(
            n => n.BulkCreateNotificationsAsync(It.IsAny<BulkCreateNotificationsDTO>()),
            Times.Never
        );
    }
}
