namespace Noo.Api.Core.System.Realtime.Jobs;

/// <summary>
/// Implemented by the client contract of any hub that reports job progress, so one client-side
/// handler serves every such hub.
/// </summary>
public interface IJobProgressClient
{
    public Task JobProgressAsync(JobProgress progress);
}
