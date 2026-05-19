using System.ComponentModel.DataAnnotations;

namespace Noo.Api.Core.Config.Env;

public class EventsConfig : IConfig
{
    public static string SectionName => "Events";

    [Range(1, 1048576)]
    public int QueueCapacity { get; set; } = 2048;

    [Range(1, 3600)]
    public int HandlerTimeoutSeconds { get; set; } = 30;

    [Range(1, 1024)]
    public int MaxConcurrentEvents { get; set; } = 8;

    [Range(1, 128)]
    public int MaxConcurrentHandlersPerEvent { get; set; } = 4;
}
