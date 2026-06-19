using Noo.Api.NooTube.Types;

namespace Noo.Api.NooTube.Engines;

/// <summary>
/// Provider-agnostic port for an external video hosting engine (Kinescope today,
/// other providers tomorrow). Each implementation declares the <see cref="ServiceType"/>
/// it handles and is selected at runtime by <see cref="IVideoEngineResolver"/>.
/// </summary>
public interface IVideoEngine
{
    public NooTubeServiceType ServiceType { get; }

    /// <summary>
    /// Initializes an upload on the provider and returns the external id plus the
    /// URL the client should stream the file to (e.g. a tus endpoint).
    /// </summary>
    public Task<VideoUploadTicket> CreateUploadAsync(
        VideoUploadRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns the current metadata for a video, or <c>null</c> if it is unknown to the provider.
    /// </summary>
    public Task<VideoMetadata?> GetMetadataAsync(
        string externalId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Permanently deletes the video on the provider.
    /// </summary>
    public Task DeleteAsync(string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Translates a provider-specific status string (e.g. from a webhook) into a
    /// provider-agnostic <see cref="VideoProcessingStatus"/>.
    /// </summary>
    public VideoProcessingStatus MapStatus(string? rawStatus);
}
