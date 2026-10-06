namespace Herald;

/// <summary>
/// Sends requests to a single handler through the pipeline behaviors.
/// </summary>
public interface ISender
{
    /// <summary>
    /// Sends a request that returns a value.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response returned by the handler.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No handler is registered for the request.</exception>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a request that does not return a value.
    /// </summary>
    /// <typeparam name="TRequest">The type of request.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No handler is registered for the request.</exception>
    Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest;

    /// <summary>
    /// Sends a request whose type is not known at compile time.
    /// </summary>
    /// <param name="request">
    /// The request to send. It must implement <see cref="IRequest"/> or <see cref="IRequest{TResponse}"/>.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response returned by the handler, or <see langword="null"/> for a request that does not return a value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="request"/> does not implement <see cref="IBaseRequest"/>.</exception>
    /// <exception cref="InvalidOperationException">No handler is registered for the request.</exception>
    Task<object?> Send(object request, CancellationToken cancellationToken = default);
}
