using System.Collections.Concurrent;

namespace Herald.Tests.Infrastructure;

/// <summary>
/// Thread-safe list that records the order in which handlers and behaviors run.
/// </summary>
public sealed class CallLog
{
    private readonly ConcurrentQueue<string> _entries = new();

    public IReadOnlyList<string> Entries => [.. _entries];

    public void Add(string entry) => _entries.Enqueue(entry);
}
