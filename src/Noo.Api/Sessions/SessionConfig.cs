using System.ComponentModel.DataAnnotations;
using Noo.Api.Core.Config;

namespace Noo.Api.Sessions;

[ModuleConfig]
public class SessionConfig : IConfig
{
    public static string SectionName => "Sessions";

    [Range(1, int.MaxValue)]
    public int OnlineTtlMinutes { get; set; } = 15;

    [Range(1, int.MaxValue)]
    public int ActiveTtlDays { get; set; } = 14;

    [Range(1, int.MaxValue)]
    public int DbUpdateThrottleMinutes { get; set; } = 5;

    [Range(1, int.MaxValue)]
    public int CleanupIntervalHours { get; set; } = 12;

    [Range(1, int.MaxValue)]
    public int SessionRetentionDays { get; set; } = 30;

    public TimeSpan OnlineTtl => TimeSpan.FromMinutes(OnlineTtlMinutes);
    public TimeSpan ActiveTtl => TimeSpan.FromDays(ActiveTtlDays);
    public TimeSpan DbUpdateThrottle => TimeSpan.FromMinutes(DbUpdateThrottleMinutes);
    public TimeSpan CleanupInterval => TimeSpan.FromHours(CleanupIntervalHours);
}
