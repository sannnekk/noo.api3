using System.Net.Sockets;

namespace Noo.IntegrationTests;

/// <summary>
/// A fact that needs a real Redis backplane, skipped when one is not listening. The two-instance
/// behaviour it covers cannot be faked: with an in-memory substitute each instance would talk
/// only to itself, which is exactly the bug the test exists to catch.
/// </summary>
public sealed class BackplaneFactAttribute : FactAttribute
{
    public const string Host = "127.0.0.1";
    public const int Port = 6380;

    public BackplaneFactAttribute()
    {
        if (!IsListening())
        {
            Skip = $"No realtime backplane on {Host}:{Port} — start it with ./services.sh";
        }
    }

    private static bool IsListening()
    {
        try
        {
            using var client = new TcpClient();

            return client.ConnectAsync(Host, Port).Wait(TimeSpan.FromMilliseconds(300))
                && client.Connected;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
