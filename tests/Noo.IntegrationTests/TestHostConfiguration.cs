using System.Runtime.CompilerServices;

namespace Noo.IntegrationTests;

internal static class TestHostConfiguration
{
    /// <summary>
    /// Stops each test host from watching the appsettings files for changes.
    /// </summary>
    /// <remarks>
    /// Every test stands up its own host, and the default host configuration
    /// registers a file watcher per JSON source. On Linux those are inotify
    /// instances, capped per user (128 by default) — so a run of this size
    /// exhausts them and hosts start failing to build with
    /// "The configured user limit on the number of inotify instances has been
    /// reached", which reads as an unrelated test failure.
    /// <para>
    /// Nothing rewrites configuration mid-run, so the watching buys the tests
    /// nothing. The switch has to be set before the first host is built, hence a
    /// module initializer: <c>WebApplication.CreateBuilder</c> reads it while
    /// applying the default configuration, before anything the factory does.
    /// </para>
    /// </remarks>
    [ModuleInitializer]
    internal static void DisableConfigurationReloading()
    {
        Environment.SetEnvironmentVariable(
            "DOTNET_hostBuilder:reloadConfigOnChange",
            "false"
        );
    }
}
