using System.Text.Json;
using Moq;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.Json;
using Noo.Api.NooTube.DTO;
using Noo.Api.NooTube.Models;
using Noo.Api.NooTube.Services;
using Noo.Api.NooTube.Types;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.NooTube;

public class VideoReactionTests
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

        public bool IsInRole(params UserRoles[] role) =>
            UserRole.HasValue && role.Contains(UserRole.Value);
    }

    private static VideoService MakeService(NooDbContext context, ICurrentUser currentUser)
    {
        return new VideoService(
            new VideoRepository(context),
            new VideoReactionRepository(context),
            Mock.Of<IVideoFavouriteRepository>(),
            Mock.Of<Api.Core.Request.Patching.IJsonPatchUpdateService>(),
            Mock.Of<Api.NooTube.Engines.IVideoEngineResolver>(),
            currentUser
        );
    }

    private static async Task<Ulid> SeedVideoAsync(NooDbContext context)
    {
        var video = new NooTubeVideoModel
        {
            Title = "Video",
            ServiceType = NooTubeServiceType.Kinescope,
            State = VideoState.Uploaded,
            IsListed = true,
        };

        context.GetDbSet<NooTubeVideoModel>().Add(video);
        await context.SaveChangesAsync();

        return video.Id;
    }

    private static Task<NooTubeVideoReactionModel?> GetReactionAsync(
        NooDbContext context,
        Ulid videoId,
        Ulid userId
    )
    {
        return new VideoReactionRepository(context).GetAsync(videoId, userId);
    }

    [Fact]
    public async Task Toggle_Creates_Reaction()
    {
        using var context = TestHelpers.CreateInMemoryDb();
        var videoId = await SeedVideoAsync(context);
        var userId = Ulid.NewUlid();
        var service = MakeService(context, new TestCurrentUser(userId, UserRoles.Student));

        await service.ToggleReactionAsync(videoId, VideoReaction.Heart);
        await context.SaveChangesAsync();

        var reaction = await GetReactionAsync(context, videoId, userId);
        Assert.NotNull(reaction);
        Assert.Equal(VideoReaction.Heart, reaction!.Reaction);
    }

    [Fact]
    public async Task Toggle_Same_Reaction_Removes_It()
    {
        using var context = TestHelpers.CreateInMemoryDb();
        var videoId = await SeedVideoAsync(context);
        var userId = Ulid.NewUlid();
        var service = MakeService(context, new TestCurrentUser(userId, UserRoles.Student));

        await service.ToggleReactionAsync(videoId, VideoReaction.Like);
        await context.SaveChangesAsync();

        await service.ToggleReactionAsync(videoId, VideoReaction.Like);
        await context.SaveChangesAsync();

        Assert.Null(await GetReactionAsync(context, videoId, userId));
    }

    [Fact]
    public async Task Toggle_Different_Reaction_Switches_It()
    {
        using var context = TestHelpers.CreateInMemoryDb();
        var videoId = await SeedVideoAsync(context);
        var userId = Ulid.NewUlid();
        var service = MakeService(context, new TestCurrentUser(userId, UserRoles.Student));

        await service.ToggleReactionAsync(videoId, VideoReaction.Like);
        await context.SaveChangesAsync();

        await service.ToggleReactionAsync(videoId, VideoReaction.Laugh);
        await context.SaveChangesAsync();

        var reactions = context
            .GetDbSet<NooTubeVideoReactionModel>()
            .Where(r => r.VideoId == videoId && r.UserId == userId)
            .ToList();

        Assert.Single(reactions);
        Assert.Equal(VideoReaction.Laugh, reactions[0].Reaction);
    }

    [Fact]
    public async Task Toggle_Throws_NotFound_For_Unknown_Video()
    {
        using var context = TestHelpers.CreateInMemoryDb();
        var service = MakeService(
            context,
            new TestCurrentUser(Ulid.NewUlid(), UserRoles.Student)
        );

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.ToggleReactionAsync(Ulid.NewUlid(), VideoReaction.Like)
        );
    }

    [Fact]
    public async Task GetReactions_Counts_Reactions_Of_All_Users()
    {
        using var context = TestHelpers.CreateInMemoryDb();
        var videoId = await SeedVideoAsync(context);
        var userId = Ulid.NewUlid();

        context
            .GetDbSet<NooTubeVideoReactionModel>()
            .AddRange(
                new NooTubeVideoReactionModel
                {
                    VideoId = videoId,
                    UserId = userId,
                    Reaction = VideoReaction.Like,
                },
                new NooTubeVideoReactionModel
                {
                    VideoId = videoId,
                    UserId = Ulid.NewUlid(),
                    Reaction = VideoReaction.Like,
                },
                new NooTubeVideoReactionModel
                {
                    VideoId = videoId,
                    UserId = Ulid.NewUlid(),
                    Reaction = VideoReaction.Sad,
                },
                new NooTubeVideoReactionModel
                {
                    VideoId = await SeedVideoAsync(context),
                    UserId = Ulid.NewUlid(),
                    Reaction = VideoReaction.Dislike,
                }
            );
        await context.SaveChangesAsync();

        var service = MakeService(context, new TestCurrentUser(userId, UserRoles.Student));
        var reactions = await service.GetReactionsAsync(videoId);

        Assert.Equal(VideoReaction.Like, reactions.MyReaction);
        Assert.Equal(2, reactions.Counts.Count);
        Assert.Equal(2, reactions.Counts[VideoReaction.Like]);
        Assert.Equal(1, reactions.Counts[VideoReaction.Sad]);
    }

    [Fact]
    public async Task GetReactions_Returns_Null_MyReaction_When_User_Did_Not_React()
    {
        using var context = TestHelpers.CreateInMemoryDb();
        var videoId = await SeedVideoAsync(context);

        context
            .GetDbSet<NooTubeVideoReactionModel>()
            .Add(
                new NooTubeVideoReactionModel
                {
                    VideoId = videoId,
                    UserId = Ulid.NewUlid(),
                    Reaction = VideoReaction.Mindblowing,
                }
            );
        await context.SaveChangesAsync();

        var service = MakeService(
            context,
            new TestCurrentUser(Ulid.NewUlid(), UserRoles.Student)
        );
        var reactions = await service.GetReactionsAsync(videoId);

        Assert.Null(reactions.MyReaction);
        Assert.Equal(1, reactions.Counts[VideoReaction.Mindblowing]);
    }

    [Fact]
    public void Reactions_Are_Serialized_With_Hyphen_Lowercase_Keys()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new HyphenLowerCaseStringEnumConverterFactory() },
        };

        var json = JsonSerializer.Serialize(
            new NooTubeVideoReactionsDTO
            {
                MyReaction = VideoReaction.Mindblowing,
                Counts = new Dictionary<VideoReaction, int>
                {
                    [VideoReaction.Like] = 2,
                    [VideoReaction.Mindblowing] = 1,
                },
            },
            options
        );

        Assert.Equal(
            """{"myReaction":"mindblowing","counts":{"like":2,"mindblowing":1}}""",
            json
        );
    }

    [Fact]
    public async Task GetReactions_Throws_NotFound_For_Unknown_Video()
    {
        using var context = TestHelpers.CreateInMemoryDb();
        var service = MakeService(
            context,
            new TestCurrentUser(Ulid.NewUlid(), UserRoles.Student)
        );

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetReactionsAsync(Ulid.NewUlid())
        );
    }
}
