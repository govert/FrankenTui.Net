using System.Text.Json;

namespace FrankenTui.Runtime;

/// <summary>Metrics registry for counters and gauges. Matches upstream metrics_registry.</summary>
public sealed class MetricsRegistry
{
    private readonly Dictionary<string, double> _counters = [];
    private readonly Dictionary<string, double> _gauges = [];

    public void Increment(string name, double delta = 1)
    {
        _counters.TryGetValue(name, out var v);
        _counters[name] = v + delta;
    }

    public void SetGauge(string name, double value) => _gauges[name] = value;
    public double GetCounter(string name) => _counters.GetValueOrDefault(name);
    public double GetGauge(string name) => _gauges.GetValueOrDefault(name);
    public IReadOnlyDictionary<string, double> Counters => _counters;
    public IReadOnlyDictionary<string, double> Gauges => _gauges;
}

/// <summary>Event trace ring buffer. Matches upstream event_trace.</summary>
public sealed class EventTrace
{
    private readonly (DateTimeOffset Time, string Event, string? Data)[] _buffer;
    private int _head;
    private int _count;

    public EventTrace(int capacity = 10000) => _buffer = new (DateTimeOffset, string, string?)[capacity];

    public void Record(string eventName, string? data = null)
    {
        _buffer[_head] = (DateTimeOffset.UtcNow, eventName, data);
        _head = (_head + 1) % _buffer.Length;
        if (_count < _buffer.Length) _count++;
    }

    public IEnumerable<string> Recent(int count = 100) =>
        Enumerate().TakeLast(count).Select(e => $"[{e.Time:HH:mm:ss.fff}] {e.Event} {e.Data ?? ""}");
    private IEnumerable<(DateTimeOffset Time, string Event, string? Data)> Enumerate()
    {
        for (var i = 0; i < _count; i++)
        {
            var idx = (_head - _count + i + _buffer.Length) % _buffer.Length;
            yield return _buffer[idx];
        }
    }
}

/// <summary>Evidence bridges for connecting telemetry to evidence pipeline. Matches upstream evidence_bridges.</summary>
public sealed class EvidenceBridge
{
    private readonly List<string> _records = [];

    public void Bridge(string source, string eventName, IReadOnlyDictionary<string, object> fields)
    {
        _records.Add(JsonSerializer.Serialize(new { source, name = eventName, fields, ts = DateTimeOffset.UtcNow }));
    }

    public IReadOnlyList<string> Records => _records;
    public void Clear() => _records.Clear();
}
