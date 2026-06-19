namespace Noo.Api.NooTube.Engines;

/// <summary>
/// Provider-agnostic processing status reported by a video engine.
/// </summary>
public enum VideoProcessingStatus
{
    Pending,
    Processing,
    Ready,
    Failed,
}
