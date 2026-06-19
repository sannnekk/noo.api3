namespace Noo.Api.NooTube;

public static class NooTubeConfig
{
    /// <summary>
    /// Run interval for the video publishing job
    /// </summary>
    public static readonly TimeSpan BackgroundJobInterval = TimeSpan.FromHours(1);
}
