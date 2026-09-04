using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.StackExchangeRedis;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Initialization.Configuration;
using Noo.Api.Core.System.Realtime;
using Noo.Api.Core.System.Realtime.Backplane;
using Noo.Api.Core.System.Realtime.Filters;
using Noo.Api.Core.Utils.Json;

namespace Noo.Api.Core.Initialization.ServiceCollection;

public static class RealtimeExtension
{
    public static IServiceCollection AddNooRealtime(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var config = configuration
            .GetSection(RealtimeConfig.SectionName)
            .GetOrThrow<RealtimeConfig>();

        if (!config.Enabled)
        {
            return services;
        }

        services.AddSingleton<IRealtimeBackplane, RealtimeBackplane>();
        services.AddSingleton<RealtimeMetrics>();
        services.AddSingleton<IUserIdProvider, NooUserIdProvider>();

        services.AddSingleton<HubExceptionFilter>();
        services.AddSingleton<HubRateLimitFilter>();
        services.AddSingleton<HubPrincipalFilter>();

        var signalR = services
            .AddSignalR(options =>
            {
                options.KeepAliveInterval = config.KeepAliveInterval;
                options.ClientTimeoutInterval = config.ClientTimeoutInterval;
                options.HandshakeTimeout = config.HandshakeTimeout;
                options.MaximumReceiveMessageSize = config.MaximumReceiveMessageSize;
                options.EnableDetailedErrors = false;

                // Order matters: the principal must be in place before anything the invocation
                // touches reads it, and the exception filter has to wrap the rest to translate
                // what they throw.
                options.AddFilter<HubPrincipalFilter>();
                options.AddFilter<HubExceptionFilter>();
                options.AddFilter<HubRateLimitFilter>();
            })
            .AddJsonProtocol(options => options.PayloadSerializerOptions.AddNooConverters());

        if (config.HasBackplane)
        {
            services.AddSingleton<IConfigureOptions<RedisOptions>, ConfigureRealtimeBackplaneOptions>();
            signalR.AddStackExchangeRedis();
        }

        return services;
    }
}
