using AutoMapper;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Users.DTO;
using Noo.Api.Users.Filters;
using Noo.Api.Users.Models;
using Noo.Api.Users.Services;
using Noo.Api.Users.Types;
using Noo.UnitTests.Common;
using SystemTextJsonPatch;

namespace Noo.UnitTests.Users;

public class UserServiceTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            // Explicit mapping for creation to avoid requiring all navigations
            cfg.CreateMap<UserCreationPayload, UserModel>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
                .ForMember(d => d.TelegramId, opt => opt.Ignore())
                .ForMember(d => d.TelegramUsername, opt => opt.Ignore())
                .ForMember(d => d.CoursesAsMember, opt => opt.Ignore())
                .ForMember(d => d.CoursesAsAssigner, opt => opt.Ignore())
                .ForMember(d => d.CoursesAsAuthor, opt => opt.Ignore())
                .ForMember(d => d.CoursesAsEditor, opt => opt.Ignore())
                .ForMember(d => d.CourseMaterialReactions, opt => opt.Ignore())
                .ForMember(d => d.Avatar, opt => opt.Ignore())
                .ForMember(d => d.Sessions, opt => opt.Ignore())
                .ForMember(d => d.Snippets, opt => opt.Ignore())
                .ForMember(d => d.PollParticipations, opt => opt.Ignore())
                .ForMember(d => d.CalendarEvents, opt => opt.Ignore())
                .ForMember(d => d.Notifications, opt => opt.Ignore())
                .ForMember(d => d.Settings, opt => opt.Ignore())
                .ForMember(d => d.UploadedVideos, opt => opt.Ignore())
                .ForMember(d => d.NooTubeVideoComments, opt => opt.Ignore())
                .ForMember(d => d.NooTubeVideoReactions, opt => opt.Ignore())
                .ForMember(d => d.AssignedWorkHistoryChanges, opt => opt.Ignore())
                .ForMember(d => d.IsBlocked, opt => opt.MapFrom(_ => false))
                .ForMember(d => d.IsVerified, opt => opt.MapFrom(_ => false));

            cfg.CreateMap<UserModel, UpdateUserDTO>();
            var map = cfg.CreateMap<UpdateUserDTO, UserModel>();
            map.ForAllMembers(opt => opt.Ignore());
            map.AfterMap((src, dest) =>
            {
                if (src.Username != null) dest.Username = src.Username;
                if (src.Email != null) dest.Email = src.Email;
                if (src.Name != null) dest.Name = src.Name;
                if (src.TelegramId != null) dest.TelegramId = src.TelegramId;
                if (src.TelegramUsername != null) dest.TelegramUsername = src.TelegramUsername;
            });
        });
        return config.CreateMapper();
    }

    private static UserCreationPayload MakePayload(string username = "john", string email = "john@example.com", string name = "John Doe", UserRoles role = UserRoles.Student)
        => new UserCreationPayload
        {
            Username = username,
            Email = email,
            PasswordHash = "hash",
            Name = name,
            Role = role
        };

    [Fact]
    public async Task Create_Get_Search_Patch_Delete_User_Flow()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(dbName);
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var service = new UserService(uow, mapper);

        var id = await service.CreateUserAsync(MakePayload());
        Assert.NotEqual(default, id);

        var fetched = await service.GetUserByIdAsync(id);
        Assert.NotNull(fetched);
        Assert.Equal("john", fetched!.Username);
        Assert.Equal("john@example.com", fetched.Email);

        var search = await service.GetUsersAsync(new UserFilter { Page = 1, PerPage = 10 });
        Assert.Equal(1, search.Total);
        Assert.Single(search.Items);

        var patch = new JsonPatchDocument<UpdateUserDTO>();
        patch.Replace(x => x.Name, "John Updated");
        await service.UpdateUserAsync(id, patch, new ModelStateDictionary());

        var updated = await service.GetUserByIdAsync(id);
        Assert.Equal("John Updated", updated!.Name);

        // Delete in a fresh context to avoid tracking conflicts
        using (var deleteContext = TestHelpers.CreateInMemoryDb(dbName))
        {
            var deleteUow = TestHelpers.CreateUowMock(deleteContext).Object;
            var deleteService = new UserService(deleteUow, mapper);
            await deleteService.DeleteUserAsync(id);
        }

        using (var verifyContext = TestHelpers.CreateInMemoryDb(dbName))
        {
            var verifyUow = TestHelpers.CreateUowMock(verifyContext).Object;
            var verifyService = new UserService(verifyUow, mapper);
            var afterDelete = await verifyService.GetUserByIdAsync(id);
            Assert.Null(afterDelete);
        }
    }

    [Fact]
    public async Task ChangeRole_Succeeds_For_Student_And_Fails_Otherwise()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(dbName);
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var service = new UserService(uow, mapper);

        var studentId = await service.CreateUserAsync(MakePayload("stud", "stud@example.com", role: UserRoles.Student));
        await service.ChangeRoleAsync(studentId, UserRoles.Mentor);

        var changed = await service.GetUserByIdAsync(studentId);
        Assert.Equal(UserRoles.Mentor, changed!.Role);

        // Make a mentor and try to change role -> should conflict
        var mentorId = await service.CreateUserAsync(MakePayload("mentor", "mentor@example.com", role: UserRoles.Mentor));
        await Assert.ThrowsAsync<CantChangeRoleException>(() => service.ChangeRoleAsync(mentorId, UserRoles.Admin));
    }

    [Fact]
    public async Task ChangeRole_And_Verify_Fails_When_Blocked_Or_NotFound()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(dbName);
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var service = new UserService(uow, mapper);

        // Not found
        await Assert.ThrowsAsync<NotFoundException>(() => service.ChangeRoleAsync(Ulid.NewUlid(), UserRoles.Mentor));

        // Blocked
        var id = await service.CreateUserAsync(MakePayload("blocked", "blocked@example.com"));

        // Mark as blocked directly and persist
        var user = await service.GetUserByIdAsync(id);
        Assert.NotNull(user);
        user!.IsBlocked = true;
        uow.Context.GetDbSet<UserModel>().Update(user);
        await uow.CommitAsync();

        await Assert.ThrowsAsync<UserIsBlockedException>(() => service.ChangeRoleAsync(id, UserRoles.Mentor));
        await Assert.ThrowsAsync<UserIsBlockedException>(() => service.VerifyUserAsync(id));
    }

    [Fact]
    public async Task Update_Email_And_Password_Work()
    {
        using var context = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var service = new UserService(uow, mapper);

        var id = await service.CreateUserAsync(MakePayload("u1", "u1@example.com"));

        await service.UpdateUserEmailAsync(id, "new@example.com");
        await service.UpdateUserPasswordAsync(id, "new-hash");

        var updated = await service.GetUserByIdAsync(id);
        Assert.Equal("new@example.com", updated!.Email);
        Assert.Equal("new-hash", updated.PasswordHash);
    }

    [Fact(Skip = "EFCore InMemory doesn't support ExecuteUpdate/ExecuteUpdateAsync used by Block/Unblock methods")]
    public async Task Block_Unblock_And_IsBlocked_Work()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(dbName);
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var service = new UserService(uow, mapper);

        var id = await service.CreateUserAsync(MakePayload("u2", "u2@example.com"));

        await service.BlockUserAsync(id);
        using (var verifyCtx = TestHelpers.CreateInMemoryDb(dbName))
        {
            var verifyUow = TestHelpers.CreateUowMock(verifyCtx).Object;
            var verifyService = new UserService(verifyUow, mapper);
            Assert.True(await verifyService.IsBlockedAsync(id));
        }

        await service.UnblockUserAsync(id);
        using (var verifyCtx2 = TestHelpers.CreateInMemoryDb(dbName))
        {
            var verifyUow2 = TestHelpers.CreateUowMock(verifyCtx2).Object;
            var verifyService2 = new UserService(verifyUow2, mapper);
            Assert.False(await verifyService2.IsBlockedAsync(id));
        }
    }

    [Fact]
    public async Task UserExists_And_GetByUsernameOrEmail()
    {
        using var context = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var service = new UserService(uow, mapper);

        await Assert.ThrowsAsync<ArgumentException>(() => service.UserExistsAsync(null, null));

        var id = await service.CreateUserAsync(MakePayload("exists-user", "exists@example.com"));

        Assert.True(await service.UserExistsAsync("exists-user", null));
        Assert.True(await service.UserExistsAsync(null, "exists@example.com"));
        Assert.True(await service.UserExistsAsync("exists-user", "exists@example.com"));
        Assert.False(await service.UserExistsAsync("nope", null));
        Assert.False(await service.UserExistsAsync(null, "nope@example.com"));

        var byUsername = await service.GetUserByUsernameOrEmailAsync("exists-user");
        Assert.NotNull(byUsername);
        var byEmail = await service.GetUserByUsernameOrEmailAsync("exists@example.com");
        Assert.NotNull(byEmail);
        var none = await service.GetUserByUsernameOrEmailAsync("missing");
        Assert.Null(none);
    }

    [Fact]
    public async Task VerifyUser_Sets_IsVerified_When_Not_Blocked()
    {
        using var context = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var service = new UserService(uow, mapper);

        var id = await service.CreateUserAsync(MakePayload("verify", "verify@example.com"));
        await service.VerifyUserAsync(id);
        var user = await service.GetUserByIdAsync(id);
        Assert.True(user!.IsVerified);
    }

    [Fact]
    public async Task UpdateUser_NotFound_Throws()
    {
        using var context = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var service = new UserService(uow, mapper);

        var patch = new JsonPatchDocument<UpdateUserDTO>().Replace(x => x.Name, "N");
        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateUserAsync(Ulid.NewUlid(), patch));
    }
}
