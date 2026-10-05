using System.Collections.Concurrent;

namespace Herald.Tests.Infrastructure;

/// <summary>
/// Handler ve behavior'ların hangi sırayla çalıştığını kaydeden, thread-safe kayıt listesi.
/// </summary>
public sealed class CallLog
{
    private readonly ConcurrentQueue<string> _entries = new();

    public IReadOnlyList<string> Entries => [.. _entries];

    public void Add(string entry) => _entries.Enqueue(entry);
}
