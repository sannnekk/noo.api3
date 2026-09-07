using Microsoft.Extensions.DependencyInjection.Extensions;
using Noo.Api.Core.DataAbstraction.Cache;
using Noo.Api.Core.Initialization.Configuration;
using Noo.Api.Core.System.Collaboration;
using Noo.Api.Core.System.Collaboration.Store;

namespace Noo.Api.Core.Initialization.ServiceCollection;

public static class CollaborationExtension
{
    /// <summary>
    /// Room state goes to its own Redis connection when one is configured, otherwise to the cache
    /// connection, otherwise to process memory. The last step is the same graceful degradation
    /// the cache makes, and for the same reason: a missing Redis should cost a feature its reach
    /// across instances, not take the API down.
    /// </summary>
    public static IServiceCollection AddNooCollaboration(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var config = configuration
            .GetSection(CollaborationConfig.SectionName)
            .GetOrThrow<CollaborationConfig>();

        if (!config.Enabled)
        {
            return services;
        }

        services.TryAddSingleton<IRedisConnectionFactory, RedisConnectionFactory>();
        services.TryAddSingleton<IRedisConnectionProvider, RedisConnectionProvider>();

        services.AddSingleton<ICollaborationStore>(sp =>
        {
            if (!string.IsNullOrWhiteSpace(config.ConnectionString))
            {
                var connection = sp.GetRequiredService<IRedisConnectionFactory>()
                    .Connect(config.ConnectionString);

                return ActivatorUtilities.CreateInstance<RedisCollaborationStore>(sp, connection);
            }

            if (sp.GetRequiredService<IRedisConnectionProvider>().TryGetConnection(out var shared))
            {
                return ActivatorUtilities.CreateInstance<RedisCollaborationStore>(sp, shared);
            }

            sp.GetRequiredService<ILogger<InMemoryCollaborationStore>>()
                .LogWarning(
                    "No Redis for collaboration rooms; falling back to process memory. "
                        + "Leases and presence will not be seen by other instances."
                );

            return ActivatorUtilities.CreateInstance<InMemoryCollaborationStore>(sp);
        });

        return services;
    }
}
