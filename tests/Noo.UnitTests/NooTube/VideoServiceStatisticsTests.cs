using Moq;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils;
using Noo.Api.NooTube.Engines;
using Noo.Api.NooTube.Exceptions;
using Noo.Api.NooTube.Models;
using Noo.Api.NooTube.Services;
using Noo.Api.NooTube.Types;

namespace Noo.UnitTests.NooTube;

public class VideoServiceStatisticsTests
{
    private static VideoService CreateService(
        Mock<IVideoRepository> videoRepository,
        Mock<IVideoEngineResolver> engineResolver
    )
    {
        return new VideoService(
            videoRepository.Object,
            Mock.Of<IVideoReactionRepository>(),
            Mock.Of<IVideoFavouriteRepository>(),
            Mock.Of<Api.Core.Request.Patching.IJsonPatchUpdateService>(),
            engineResolver.Object,
            Mock.Of<ICurrentUser>()
        );
    }

    [Fact]
    public async Task GetStatisticsAsync_ResolvesEngine_AndReturnsStatistics()
    {
        var videoId = Ulid.NewUlid();
        var model = new NooTubeVideoModel
        {
            Id = videoId,
            Title = "Video",
            ServiceType = NooTubeServiceType.Kinescope,
            ExternalIdentifier = "ext-123",
            CreatedAt = new DateTime(2026, 1, 1),
        };

        var expected = new VideoStatistics
        {
            From = new DateTime(2026, 1, 1),
            To = Clock.Today,
            Views = 42,
        };

        var videoRepository = new Mock<IVideoRepository>();
        videoRepository.Setup(r => r.GetByIdAsync(videoId)).ReturnsAsync(model);

        var engine = new Mock<IVideoEngine>();
        engine
            .Setup(e =>
                e.GetStatisticsAsync(
                    "ext-123",
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(expected);

        var engineResolver = new Mock<IVideoEngineResolver>();
        engineResolver.Setup(r => r.Resolve(NooTubeServiceType.Kinescope)).Returns(engine.Object);

        var service = CreateService(videoRepository, engineResolver);

        var result = await service.GetStatisticsAsync(videoId, null, null);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetStatisticsAsync_DefaultsRange_ToVideoLifetime()
    {
        var videoId = Ulid.NewUlid();
        var createdAt = new DateTime(2026, 2, 10);
        var model = new NooTubeVideoModel
        {
            Id = videoId,
            Title = "Video",
            ServiceType = NooTubeServiceType.Kinescope,
            ExternalIdentifier = "ext-123",
            CreatedAt = createdAt,
        };

        var videoRepository = new Mock<IVideoRepository>();
        videoRepository.Setup(r => r.GetByIdAsync(videoId)).ReturnsAsync(model);

        DateTime? capturedFrom = null;
        DateTime? capturedTo = null;

        var engine = new Mock<IVideoEngine>();
        engine
            .Setup(e =>
                e.GetStatisticsAsync(
                    It.IsAny<string>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<string, DateTime, DateTime, CancellationToken>(
                (_, from, to, _) =>
                {
                    capturedFrom = from;
                    capturedTo = to;
                }
            )
            .ReturnsAsync((VideoStatistics?)null);

        var engineResolver = new Mock<IVideoEngineResolver>();
        engineResolver.Setup(r => r.Resolve(NooTubeServiceType.Kinescope)).Returns(engine.Object);

        var service = CreateService(videoRepository, engineResolver);

        var result = await service.GetStatisticsAsync(videoId, null, null);

        Assert.Equal(createdAt, capturedFrom);
        Assert.Equal(Clock.Today, capturedTo);
        Assert.Equal(createdAt, result.From);
        Assert.Equal(Clock.Today, result.To);
    }

    [Fact]
    public async Task GetStatisticsAsync_Throws_WhenVideoMissing()
    {
        var videoId = Ulid.NewUlid();
        var videoRepository = new Mock<IVideoRepository>();
        videoRepository.Setup(r => r.GetByIdAsync(videoId)).ReturnsAsync((NooTubeVideoModel?)null);

        var service = CreateService(videoRepository, new Mock<IVideoEngineResolver>());

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetStatisticsAsync(videoId, null, null)
        );
    }

    [Fact]
    public async Task GetStatisticsAsync_Throws_WhenNotYetUploaded()
    {
        var videoId = Ulid.NewUlid();
        var model = new NooTubeVideoModel
        {
            Id = videoId,
            Title = "Video",
            ServiceType = NooTubeServiceType.Kinescope,
            ExternalIdentifier = null,
        };

        var videoRepository = new Mock<IVideoRepository>();
        videoRepository.Setup(r => r.GetByIdAsync(videoId)).ReturnsAsync(model);

        var service = CreateService(videoRepository, new Mock<IVideoEngineResolver>());

        await Assert.ThrowsAsync<EncodingNotFinishedYetException>(
            () => service.GetStatisticsAsync(videoId, null, null)
        );
    }
}
