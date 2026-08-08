using System.ComponentModel.DataAnnotations;

namespace Noo.Api.Core.Config.Env;

public class EventsConfig : IConfig
{
    public static string SectionName => "Events";

    [Range(1, 1048576)]
    public int QueueCapacity { get; set; } = 2048;

    [Range(1, 3600)]
    public int HandlerTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// How long a publisher waits for room when the queue is saturated before the event is dropped.
    /// </summary>
    [Range(1, 60)]
    public int EnqueueTimeoutSeconds { get; set; } = 2;

    [Range(1, 1024)]
    public int MaxConcurrentEvents { get; set; } = 8;

    [Range(1, 128)]
    public int MaxConcurrentHandlersPerEvent { get; set; } = 4;
}
