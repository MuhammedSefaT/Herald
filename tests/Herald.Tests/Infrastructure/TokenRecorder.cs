using System.Collections.Concurrent;

namespace Herald.Tests.Infrastructure;

/// <summary>
/// Records the <see cref="CancellationToken"/> that reaches each layer of the pipeline.
/// </summary>
public sealed class TokenRecorder
{
    private readonly ConcurrentDictionary<string, CancellationToken> _tokens = new();

    /// <summary>
    /// The token a behavior passes on with next(token).
    /// </summary>
    public CancellationToken ReplacementToken { get; set; }

    public void Record(string step, CancellationToken cancellationToken) => _tokens[step] = cancellationToken;

    public CancellationToken Get(string step) => _tokens[step];
}
