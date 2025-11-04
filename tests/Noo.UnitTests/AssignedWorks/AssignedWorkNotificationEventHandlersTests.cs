using Moq;
using Noo.Api.AssignedWorks.Events;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Notifications.DTO;
using Noo.Api.Notifications.Services;
using Noo.Api.Users.Models;
using Noo.Api.Works.Types;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.AssignedWorks;

public class AssignedWorkNotificationEventHandlersTests
{
    private static (AssignedWorkNotificationEventHandlers handler, NooDbContext ctx, Mock<IUnitOfWork> uowMock, Mock<INotificationService> notif, Mock<IAssignedWorkLinkGenerator> linkGen) Create()
    {
        var ctx = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(ctx);
        var notif = new Mock<INotificationService>();
        var linkGen = new Mock<IAssignedWorkLinkGenerator>();
        linkGen.Setup(l => l.GenerateViewLink(It.IsAny<Ulid>())).Returns("link");
        var handler = new AssignedWorkNotificationEventHandlers(notif.Object, uow.Object, linkGen.Object);
        return (handler, ctx, uow, notif, linkGen);
    }

    private static AssignedWorkModel Seed(NooDbContext ctx, bool withHelper = false)
    {
        var student = new UserModel { Name = "S", Username = "stu", Email = "s@e.com", PasswordHash = "p", Role = UserRoles.Student };
        var mentor = new UserModel { Name = "M", Username = "men", Email = "m@e.com", PasswordHash = "p", Role = UserRoles.Mentor };
        var helper = new UserModel { Name = "H", Username = "hel", Email = "h@e.com", PasswordHash = "p", Role = UserRoles.Mentor };
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
            CheckDeadlineAt = DateTime.UtcNow.AddDays(2)
        };
        ctx.GetDbSet<AssignedWorkModel>().Add(aw);
        ctx.SaveChanges();
        return aw;
    }

    [Fact]
    public async Task AssignedWorkSolvedEvent_Creates_Notifications()
    {
        var (handler, ctx, _, notif, _) = Create();
        var aw = Seed(ctx, withHelper: true);
        await handler.Handle(new AssignedWorkSolvedEvent(aw.Id), CancellationToken.None);
        notif.Verify(n => n.BulkCreateNotificationsAsync(It.IsAny<BulkCreateNotificationsDTO>()), Times.Once);
    }

    [Fact]
    public async Task CheckDeadlineShiftedEvent_Creates_Notifications_When_Data_Valid()
    {
        var (handler, ctx, _, notif, _) = Create();
        var aw = Seed(ctx, withHelper: true);
        // need user who shifts deadline
        var user = new UserModel { Name = "Shifter", Username = "sh", Email = "sh@e.com", PasswordHash = "p", Role = UserRoles.Mentor };
        ctx.GetDbSet<UserModel>().Add(user); ctx.SaveChanges();
        await handler.Handle(new AssignedWorkCheckDeadlineShiftedEvent(aw.Id, user.Id, false), CancellationToken.None);
        notif.Verify(n => n.BulkCreateNotificationsAsync(It.IsAny<BulkCreateNotificationsDTO>()), Times.AtLeastOnce);
    }
}
