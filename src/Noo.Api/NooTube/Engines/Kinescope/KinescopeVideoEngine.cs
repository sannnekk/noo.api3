using Noo.Api.Core.ThirdPartyServices.Kinescope;
using Noo.Api.Core.ThirdPartyServices.Kinescope.Models;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.DI;
using Noo.Api.NooTube.Types;

namespace Noo.Api.NooTube.Engines.Kinescope;

[RegisterScoped(typeof(IVideoEngine))]
public class KinescopeVideoEngine : IVideoEngine
{
    private readonly IKinescopeClient _client;

    public KinescopeVideoEngine(IKinescopeClient client)
    {
        _client = client;
    }

    public NooTubeServiceType ServiceType => NooTubeServiceType.Kinescope;

    public async Task<VideoUploadTicket> CreateUploadAsync(
        VideoUploadRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var result = await _client.CreateUploadAsync(
            new CreateUploadRequest
            {
                Type = KinescopeUploadType.Video,
                Title = request.Title,
                Description = request.Description,
                FileSize = request.FileSize,
                FileName = request.FileName,
            },
            cancellationToken
        );

        return new VideoUploadTicket
        {
            ExternalId = result.Id,
            UploadUrl = result.Endpoint,
        };
    }

    public async Task<VideoMetadata?> GetMetadataAsync(
        string externalId,
        CancellationToken cancellationToken = default
    )
    {
        var video = await _client.GetVideoAsync(externalId, cancellationToken);

        if (video is null)
        {
            return null;
        }

        return new VideoMetadata
        {
            Status = MapStatus(video.Status),
            Url = video.PlayLink ?? video.EmbedLink,
            ThumbnailUrl = video.Poster?.Original,
            DurationSeconds = video.Duration is > 0 ? (int)Math.Round(video.Duration.Value) : null,
        };
    }

    public Task DeleteAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return _client.DeleteVideoAsync(externalId, cancellationToken);
    }

    public async Task<VideoStatistics?> GetStatisticsAsync(
        string externalId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default
    )
    {
        var overview = await _client.GetVideoAnalyticsOverviewAsync(
            externalId,
            DateOnly.FromDateTime(from),
            DateOnly.FromDateTime(to),
            cancellationToken
        );

        if (overview is null)
        {
            return null;
        }

        var total = overview.Total;

        return new VideoStatistics
        {
            From = from,
            To = to,
            Views = total?.Views ?? 0,
            UniqueViews = total?.UniqueViews ?? 0,
            WatchTimeSeconds = (int)Math.Round(total?.WatchTime ?? 0),
            PlayerLoads = total?.PlayerLoads ?? 0,
            Timeline = overview
                .Timeline.Select(point => new VideoStatisticsPoint
                {
                    Date = point.Date is { } date ? Clock.ToMoscow(date) : default,
                    Views = point.Views,
                    UniqueViews = point.UniqueViews,
                    WatchTimeSeconds = (int)Math.Round(point.WatchTime),
                    PlayerLoads = point.PlayerLoads,
                })
                .ToList(),
        };
    }

    public VideoProcessingStatus MapStatus(string? rawStatus)
    {
        return rawStatus?.ToLowerInvariant() switch
        {
            "done" => VideoProcessingStatus.Ready,
            "error" or "aborted" => VideoProcessingStatus.Failed,
            "pending" or "uploading" => VideoProcessingStatus.Pending,
            _ => VideoProcessingStatus.Processing,
        };
    }
}
