using Moq;
using Noo.Api.Core.Storage;
using Noo.Api.Media.DTO;
using Noo.Api.Media.Services;
using Noo.Api.Media.Types;

namespace Noo.UnitTests.Media;

public class MediaUrlPresignerTests
{
    private static Mock<IS3Storage> S3()
    {
        var mock = new Mock<IS3Storage>();
        mock.Setup(s => s.CreatePresignedDownloadAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns((string key, TimeSpan? _, CancellationToken _) => Task.FromResult($"signed::{key}"));
        return mock;
    }

    private static MediaDTO Media(string path, MediaStatus status = MediaStatus.Completed) =>
        new() { Id = Ulid.NewUlid(), Path = path, Status = status };

    [Fact]
    public async Task Signs_Completed_Media_With_Presigned_Url()
    {
        var s3 = S3();
        var media = Media("file/a");

        await new MediaUrlPresigner(s3.Object).SignAsync([media]);

        Assert.Equal("signed::file/a", media.Url);
    }

    [Fact]
    public async Task Signs_Each_Distinct_Path_Once_But_Fills_All_Duplicates()
    {
        var s3 = S3();
        var first = Media("same");
        var second = Media("same");

        await new MediaUrlPresigner(s3.Object).SignAsync([first, second]);

        Assert.Equal("signed::same", first.Url);
        Assert.Equal("signed::same", second.Url);
        s3.Verify(s => s.CreatePresignedDownloadAsync("same", It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Skips_Non_Completed_Media()
    {
        var s3 = S3();
        var pending = Media("pending", MediaStatus.Pending);

        await new MediaUrlPresigner(s3.Object).SignAsync([pending]);

        Assert.Equal(string.Empty, pending.Url);
        s3.Verify(s => s.CreatePresignedDownloadAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Empty_Input_Does_Nothing()
    {
        var s3 = S3();

        await new MediaUrlPresigner(s3.Object).SignAsync([]);

        s3.Verify(s => s.CreatePresignedDownloadAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
