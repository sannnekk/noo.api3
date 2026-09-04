using System.ComponentModel.DataAnnotations;
using Noo.Api.Core.Config;

namespace Noo.Api.Core.System.Realtime;

[ModuleConfig]
public class RealtimeConfig : IConfig
{
    public static string SectionName => "Realtime";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Deliberately its own Redis rather than the cache one: SignalR's backplane is classic
    /// pub/sub, which ignores database numbering, so a shared instance offers no isolation and
    /// backplane fan-out would compete with cache traffic for the same memory and CPU.
    /// Leave empty to run without a backplane, which is correct for a single instance only.
    /// </summary>
    public string? BackplaneConnectionString { get; set; }

    public string ChannelPrefix { get; set; } = "noo:rt:";

    [Range(1, 300)]
    public int KeepAliveSeconds { get; set; } = 30;

    [Range(1, 600)]
    public int ClientTimeoutSeconds { get; set; } = 60;

    [Range(1, 120)]
    public int HandshakeTimeoutSeconds { get; set; } = 15;

    [Range(1024, 1048576)]
    public int MaximumReceiveMessageSize { get; set; } = 32768;

    [Range(1024, 1048576)]
    public int ApplicationMaxBufferSize { get; set; } = 16384;

    [Range(1024, 1048576)]
    public int TransportMaxBufferSize { get; set; } = 16384;

    /// <summary>
    /// Hub invocations never reach the HTTP rate limiter, so they are bounded separately.
    /// </summary>
    [Range(1, 100000)]
    public int InvocationsPerMinutePerConnection { get; set; } = 120;

    /// <summary>
    /// How many connections one broadcast is delivered to at a time. A single fan-out to every
    /// connection is the one operation that spikes the whole fleet at once.
    /// </summary>
    [Range(1, 100000)]
    public int BroadcastChunkSize { get; set; } = 500;

    [Range(0, 10000)]
    public int BroadcastChunkDelayMs { get; set; } = 50;

    public bool HasBackplane => !string.IsNullOrWhiteSpace(BackplaneConnectionString);

    public TimeSpan KeepAliveInterval => TimeSpan.FromSeconds(KeepAliveSeconds);

    public TimeSpan ClientTimeoutInterval => TimeSpan.FromSeconds(ClientTimeoutSeconds);

    public TimeSpan HandshakeTimeout => TimeSpan.FromSeconds(HandshakeTimeoutSeconds);
}
