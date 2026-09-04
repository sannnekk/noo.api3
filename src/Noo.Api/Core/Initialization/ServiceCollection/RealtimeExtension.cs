using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.StackExchangeRedis;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Initialization.Configuration;
using Noo.Api.Core.System.Realtime;
using Noo.Api.Core.System.Realtime.Backplane;
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

        var signalR = services
            .AddSignalR(options =>
            {
                options.KeepAliveInterval = config.KeepAliveInterval;
                options.ClientTimeoutInterval = config.ClientTimeoutInterval;
                options.HandshakeTimeout = config.HandshakeTimeout;
                options.MaximumReceiveMessageSize = config.MaximumReceiveMessageSize;
                options.EnableDetailedErrors = false;
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
