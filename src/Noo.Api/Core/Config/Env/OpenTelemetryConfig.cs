using System.ComponentModel.DataAnnotations;

namespace Noo.Api.Core.Config.Env;

public class OpenTelemetryConfig : IConfig
{
    public static string SectionName => "OpenTelemetry";

    public bool Enabled { get; init; } = true;

    [Required]
    public string ServiceName { get; init; } = "noo-api";

    public string? ServiceNamespace { get; init; } = "noo";

    [Required]
    public string Endpoint { get; init; } = "http://localhost:4317";

    public string Protocol { get; init; } = "Grpc";

    public string? Headers { get; init; }

    public bool EmitTraces { get; init; } = true;

    public bool EmitMetrics { get; init; } = true;

    public bool EmitLogs { get; init; } = true;
}
