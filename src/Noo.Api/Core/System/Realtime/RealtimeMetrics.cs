using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Noo.Api.Core.System.Realtime;

/// <summary>
/// Connection count is the signal to scale hub pods on: idle sockets cost memory and file
/// descriptors but almost no CPU, so CPU-based autoscaling never sees them.
/// </summary>
public sealed class RealtimeMetrics : IDisposable
{
    public const string MeterName = "Noo.Realtime";

    private readonly ConcurrentDictionary<string, int> _connectionsByHub = new(StringComparer.Ordinal);
    private readonly Meter _meter;
    private readonly Counter<long> _messagesSent;

    public RealtimeMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _meter.CreateObservableGauge(
            "noo.realtime.connections",
            ObserveConnections,
            unit: "{connection}",
            description: "Hub connections currently held by this instance."
        );

        _messagesSent = _meter.CreateCounter<long>(
            "noo.realtime.messages_sent",
            unit: "{message}",
            description: "Messages handed to the hub for delivery."
        );
    }

    public int TotalConnections => _connectionsByHub.Values.Sum();

    public int ConnectionsFor(string hub) =>
        _connectionsByHub.TryGetValue(hub, out var count) ? count : 0;

    public void ConnectionOpened(string hub) =>
        _connectionsByHub.AddOrUpdate(hub, 1, (_, count) => count + 1);

    public void ConnectionClosed(string hub) =>
        _connectionsByHub.AddOrUpdate(hub, 0, (_, count) => Math.Max(0, count - 1));

    public void MessageSent(string hub, string method, int recipients = 1) =>
        _messagesSent.Add(
            recipients,
            new KeyValuePair<string, object?>("hub", hub),
            new KeyValuePair<string, object?>("method", method)
        );

    private IEnumerable<Measurement<int>> ObserveConnections() =>
        _connectionsByHub.Select(entry =>
            new Measurement<int>(entry.Value, new KeyValuePair<string, object?>("hub", entry.Key))
        );

    public void Dispose()
    {
        _meter.Dispose();
    }
}
