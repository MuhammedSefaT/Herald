using System.Collections.Concurrent;

namespace Herald.Tests.Infrastructure;

/// <summary>
/// Pipeline'ın her katmanına ulaşan <see cref="CancellationToken"/> değerlerini kaydeder.
/// </summary>
public sealed class TokenRecorder
{
    private readonly ConcurrentDictionary<string, CancellationToken> _tokens = new();

    /// <summary>
    /// Behavior'ın next(token) ile ileteceği token.
    /// </summary>
    public CancellationToken ReplacementToken { get; set; }

    public void Record(string step, CancellationToken cancellationToken) => _tokens[step] = cancellationToken;

    public CancellationToken Get(string step) => _tokens[step];
}
