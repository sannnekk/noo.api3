using Microsoft.Extensions.DependencyInjection;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Sessions.Models;
using Noo.Api.Users.Models;
using Noo.Api.Core.Utils.UserAgent;
using Noo.Api.Works.Types;

namespace Noo.IntegrationTests;

public static class TestDataHelpers
{
    public static Ulid GetUserId(ApiFactory factory, string username)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
        var users = db.GetDbSet<UserModel>();
        var user = users.First(u => u.Username == username);
        return user.Id;
    }

    public static async Task<Ulid> CreateUserAsync(
        ApiFactory factory,
        string username,
        string email,
        string password,
        UserRoles role = UserRoles.Student,
        bool isVerified = true,
        bool isBlocked = false)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
        var hashService = scope.ServiceProvider.GetRequiredService<IHashService>();

        var model = new UserModel
        {
            Name = username,
            Username = username,
            Email = email,
            PasswordHash = hashService.Hash(password),
            Role = role,
            IsVerified = isVerified,
            IsBlocked = isBlocked,
        };

        db.GetDbSet<UserModel>().Add(model);
        await db.SaveChangesAsync();
        return model.Id;
    }

    public static UserModel? FindUser(ApiFactory factory, string username)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
        return db.GetDbSet<UserModel>().FirstOrDefault(u => u.Username == username);
    }

    public static async Task<Ulid> CreateSessionAsync(ApiFactory factory, Ulid userId, string? deviceId = null, string? userAgent = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
        var set = db.GetDbSet<SessionModel>();

        var model = new SessionModel
        {
            UserId = userId,
            DeviceId = deviceId,
            UserAgent = userAgent ?? "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36",
            Browser = "Chrome",
            Os = "Linux",
            DeviceType = DeviceType.Desktop,
            LastRequestAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        set.Add(model);
        await db.SaveChangesAsync();
        return model.Id;
    }

    public static bool SessionExists(ApiFactory factory, Ulid sessionId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
        var set = db.GetDbSet<SessionModel>();
        return set.Any(s => s.Id == sessionId);
    }

    public static async Task<Ulid> CreateAssignedWorkAsync(ApiFactory factory, Ulid studentId, Ulid mentorId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();

        var model = new AssignedWorkModel
        {
            Title = $"AW-{Guid.NewGuid():N}",
            Type = WorkType.Test,
            Attempt = 1,
            StudentId = studentId,
            MainMentorId = mentorId,
            MaxScore = 10,
            SolveDeadlineAt = DateTime.UtcNow.AddDays(1),
            CheckDeadlineAt = DateTime.UtcNow.AddDays(2),
        };

        db.GetDbSet<AssignedWorkModel>().Add(model);
        await db.SaveChangesAsync();
        return model.Id;
    }

    public static async Task AddHistoryEntryAsync(
        ApiFactory factory,
        Ulid assignedWorkId,
        AssignedWorkHistoryType type,
        Ulid? changedById = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();

        db.GetDbSet<AssignedWorkHistoryModel>().Add(new AssignedWorkHistoryModel
        {
            AssignedWorkId = assignedWorkId,
            Type = type,
            ChangedAt = DateTime.UtcNow,
            ChangedById = changedById,
        });
        await db.SaveChangesAsync();
    }
}
