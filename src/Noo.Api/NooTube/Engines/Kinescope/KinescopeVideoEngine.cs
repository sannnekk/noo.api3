using Noo.Api.Core.ThirdPartyServices.Kinescope;
using Noo.Api.Core.ThirdPartyServices.Kinescope.Models;
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
