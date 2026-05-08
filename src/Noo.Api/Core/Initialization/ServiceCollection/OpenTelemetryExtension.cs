using System.Reflection;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.DataAbstraction.Cache;
using Noo.Api.Core.Initialization.Configuration;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Noo.Api.Core.Initialization.ServiceCollection;

public static class OpenTelemetryExtension
{
    public static IServiceCollection AddNooOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(OpenTelemetryConfig.SectionName);
        if (!section.Exists())
        {
            return services;
        }

        var cfg = section.GetOrThrow<OpenTelemetryConfig>();
        if (!cfg.Enabled)
        {
            return services;
        }

        var version = typeof(OpenTelemetryExtension).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(OpenTelemetryExtension).Assembly.GetName().Version?.ToString()
            ?? "0.0.0";

        var resourceConfigurator = (ResourceBuilder r) => r
            .AddService(
                serviceName: cfg.ServiceName,
                serviceNamespace: cfg.ServiceNamespace,
                serviceVersion: version)
            .AddEnvironmentVariableDetector();

        var otel = services.AddOpenTelemetry().ConfigureResource(b => resourceConfigurator(b));

        if (cfg.EmitTraces)
        {
            otel.WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(o =>
                {
                    o.RecordException = true;
                    o.Filter = ctx =>
                        !ctx.Request.Path.StartsWithSegments("/healthz") &&
                        !ctx.Request.Path.StartsWithSegments("/health");
                })
                .AddHttpClientInstrumentation(o => o.RecordException = true)
                .AddEntityFrameworkCoreInstrumentation()
                .AddRedisInstrumentation()
                .ConfigureRedisInstrumentation((sp, instr) =>
                {
                    var provider = sp.GetService<IRedisConnectionProvider>();
                    if (provider is not null && provider.TryGetConnection(out var connection))
                    {
                        instr.AddConnection(connection);
                    }
                })
                .AddOtlpExporter(o => ConfigureOtlp(o, cfg)));
        }

        if (cfg.EmitMetrics)
        {
            otel.WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter(o => ConfigureOtlp(o, cfg)));
        }

        if (cfg.EmitLogs)
        {
            services.AddLogging(loggingBuilder => loggingBuilder.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
                options.ParseStateValues = true;
                options.SetResourceBuilder(resourceConfigurator(ResourceBuilder.CreateDefault()));
                options.AddOtlpExporter(o => ConfigureOtlp(o, cfg));
            }));
        }

        return services;
    }

    private static void ConfigureOtlp(OtlpExporterOptions options, OpenTelemetryConfig cfg)
    {
        options.Endpoint = new Uri(cfg.Endpoint);

        if (Enum.TryParse<OtlpExportProtocol>(cfg.Protocol, ignoreCase: true, out var protocol))
        {
            options.Protocol = protocol;
        }

        if (!string.IsNullOrWhiteSpace(cfg.Headers))
        {
            options.Headers = cfg.Headers;
        }
    }
}
