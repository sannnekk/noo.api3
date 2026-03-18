using System.ComponentModel.DataAnnotations;
using System.Threading.RateLimiting;

namespace Noo.Api.Core.Config.Env;

public class RateLimitingConfig : IConfig
{
    public static string SectionName => "RateLimiting";

    [Required]
    public FixedWindowRateLimitPolicyConfig Global { get; set; } = new()
    {
        PermitLimit = 300,
        WindowSeconds = 60
    };

    [Required]
    public FixedWindowRateLimitPolicyConfig Login { get; set; } = new()
    {
        PermitLimit = 150,
        WindowSeconds = 60
    };

    [Required]
    public FixedWindowRateLimitPolicyConfig Registration { get; set; } = new()
    {
        PermitLimit = 30,
        WindowSeconds = 3600
    };
}

public class FixedWindowRateLimitPolicyConfig
{
    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; }

    [Range(1, int.MaxValue)]
    public int WindowSeconds { get; set; }

    [Range(0, int.MaxValue)]
    public int QueueLimit { get; set; } = 0;

    public QueueProcessingOrder QueueProcessingOrder { get; set; } = QueueProcessingOrder.OldestFirst;
}
