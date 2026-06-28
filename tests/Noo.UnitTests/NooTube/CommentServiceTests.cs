using AutoMapper;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.NooTube.DTO;
using Noo.Api.NooTube.Filters;
using Noo.Api.NooTube.Models;
using Noo.Api.NooTube.Services;
using Noo.Api.Users.Models;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.NooTube;

public class CommentServiceTests
{
    private sealed class TestCurrentUser : ICurrentUser
    {
        public TestCurrentUser(Ulid? userId, UserRoles? role = null, bool isAuthenticated = true)
        {
            UserId = userId;
            UserRole = role;
            IsAuthenticated = isAuthenticated;
        }

        public Ulid? UserId { get; }
        public UserRoles? UserRole { get; }
        public bool IsAuthenticated { get; }
        public bool IsInRole(params UserRoles[] role) => UserRole.HasValue && role.Contains(UserRole.Value);
    }

    private static IMapper CreateMapper()
    {
        var config = MapperTestUtils.CreateMapperConfig(cfg => cfg.AddProfile<NooTubeMapperProfile>());
        return config.CreateMapper();
    }

    [Fact]
    public async Task CreateComment_Persists_VideoId_From_Route()
    {
        using var context = TestHelpers.CreateInMemoryDb();
        var mapper = CreateMapper();
        var repo = new CommentRepository(context);
        var userId = Ulid.NewUlid();
        var currentUser = new TestCurrentUser(userId, UserRoles.Student);
        var jsonPatch = new JsonPatchUpdateService(mapper);
        var service = new CommentService(repo, jsonPatch, currentUser, mapper);

        var user = new UserModel
        {
            Id = userId,
            Username = "john",
            Email = "john@example.com",
            Name = "John",
            PasswordHash = "hash",
            Role = UserRoles.Student,
            IsVerified = true,
            IsBlocked = false
        };
        context.GetDbSet<UserModel>().Add(user);
        await context.SaveChangesAsync();

        var videoId = Ulid.NewUlid();

        service.CreateComment(videoId, new CreateNooTubeVideoCommentDTO { Content = "Nice video" });
        await context.SaveChangesAsync();

        var result = await service.GetAsync(new CommentFilter { Page = 1, PerPage = 10, VideoId = videoId });

        var comment = Assert.Single(result.Items);
        Assert.Equal(videoId, comment.VideoId);
        Assert.Equal(userId, comment.UserId);
        Assert.Equal("Nice video", comment.Content);
    }
}
