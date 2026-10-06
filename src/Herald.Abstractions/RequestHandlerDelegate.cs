namespace Herald;

/// <summary>
/// Represents the next step in the pipeline: the next behavior or, innermost, the request handler.
/// </summary>
/// <param name="cancellationToken">
/// The cancellation token to pass to the next step. If it is omitted, the token that the behavior received is passed on.
/// </param>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <returns>The response to the request.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);
