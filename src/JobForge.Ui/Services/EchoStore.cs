namespace JobForge.Ui.Services;

public sealed record EchoEntry(DateTimeOffset ReceivedAt, string ContentType, string UserAgent, string Body);

/// <summary>Last webhook hits received on POST /echo, so a test job leaves a visible trace.</summary>
public sealed class EchoStore
{
    private const int Capacity = 20;
    private readonly Queue<EchoEntry> _entries = new();

    public void Add(EchoEntry entry)
    {
        lock (_entries)
        {
            _entries.Enqueue(entry);
            while (_entries.Count > Capacity) _entries.Dequeue();
        }
    }

    public IReadOnlyList<EchoEntry> Snapshot()
    {
        lock (_entries) return [.. _entries];
    }
}
