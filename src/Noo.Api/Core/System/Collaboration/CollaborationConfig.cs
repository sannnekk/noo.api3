using System.ComponentModel.DataAnnotations;
using Noo.Api.Core.Config;

namespace Noo.Api.Core.System.Collaboration;

[ModuleConfig]
public class CollaborationConfig : IConfig
{
    public static string SectionName => "Collaboration";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Where room state lives. Empty means the cache Redis, which is the right default: room
    /// state is small, short-lived and read far more than written, exactly like the cache, and
    /// unlike the backplane it is real key-value data rather than pub/sub.
    /// Leave the cache unconfigured too and rooms fall back to process memory, which is correct
    /// for a single instance only.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// How long a field stays claimed without a heartbeat. Short enough that a killed tab frees
    /// the field while the other editor is still looking at it, long enough to survive one lost
    /// heartbeat.
    /// </summary>
    [Range(5, 300)]
    public int LeaseTtlSeconds { get; set; } = 20;

    /// <summary>
    /// How long a member survives in the roster without a heartbeat. Covers the case a graceful
    /// disconnect cannot: an instance that died holding the connection.
    /// </summary>
    [Range(10, 600)]
    public int PresenceTtlSeconds { get; set; } = 45;

    /// <summary>
    /// How long an unsaved draft outlives the last person editing it.
    /// </summary>
    [Range(1, 168)]
    public int DraftTtlHours { get; set; } = 4;

    /// <summary>
    /// When the op log passes this, it is compacted into a single whole-document operation.
    /// Without it a long session grows the draft without bound.
    /// </summary>
    [Range(50, 100000)]
    public int MaxOps { get; set; } = 2000;

    [Range(2, 200)]
    public int MaxMembersPerRoom { get; set; } = 20;

    [Range(1024, 4194304)]
    public int MaxOpBytes { get; set; } = 65536;

    /// <summary>
    /// Caps one relayed CRDT frame. Frames are coalesced by the client, so this bounds a burst of
    /// typing rather than a keystroke; anything larger is a bulk sync and belongs on HTTP.
    /// </summary>
    [Range(1024, 1048576)]
    public int MaxYjsFrameBytes { get; set; } = 16384;

    public TimeSpan LeaseTtl => TimeSpan.FromSeconds(LeaseTtlSeconds);

    public TimeSpan PresenceTtl => TimeSpan.FromSeconds(PresenceTtlSeconds);

    public TimeSpan DraftTtl => TimeSpan.FromHours(DraftTtlHours);
}
