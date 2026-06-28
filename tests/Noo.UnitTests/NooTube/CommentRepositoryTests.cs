using Noo.Api.Core.Security.Authorization;
using Noo.Api.NooTube.Filters;
using Noo.Api.NooTube.Models;
using Noo.Api.NooTube.Services;
using Noo.Api.NooTube.Specifications;
using Noo.Api.Users.Models;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.NooTube;

public class CommentRepositoryTests
{
    [Fact]
    public void Constructor_Sets_Context()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();

        var repo = new CommentRepository(ctx);

        Assert.NotNull(repo.Context);
    }

    [Fact]
    public async Task SearchAsync_Does_Not_Throw_NullReference()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var repo = new CommentRepository(ctx);

        var user = new UserModel
        {
            Id = Ulid.NewUlid(),
            Username = "john",
            Email = "john@example.com",
            Name = "John",
            PasswordHash = "hash",
            Role = UserRoles.Student,
            IsVerified = true,
            IsBlocked = false
        };
        var videoId = Ulid.NewUlid();
        var comment = new NooTubeVideoCommentModel
        {
            Id = Ulid.NewUlid(),
            VideoId = videoId,
            UserId = user.Id,
            Content = "Nice video"
        };

        ctx.GetDbSet<UserModel>().Add(user);
        ctx.GetDbSet<NooTubeVideoCommentModel>().Add(comment);
        await ctx.SaveChangesAsync();

        var filter = new CommentFilter { Page = 1, PerPage = 10, VideoId = videoId };

        var result = await repo.SearchAsync(filter, [new CommentSpecification()]);

        Assert.Equal(1, result.Total);
        Assert.Single(result.Items);
    }
}
