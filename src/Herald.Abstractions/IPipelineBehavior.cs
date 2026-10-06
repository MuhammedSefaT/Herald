namespace Herald;

/// <summary>
/// A pipeline step that runs between the sender and the handler. Use it for cross-cutting concerns such as
/// logging, validation or transactions. Requests that do not return a value appear here with
/// <typeparamref name="TResponse"/> = <see cref="Unit"/>.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : notnull
{
    /// <summary>
    /// Handles the request. Call <paramref name="next"/> to continue the pipeline.
    /// </summary>
    /// <param name="request">The request being handled.</param>
    /// <param name="next">
    /// The next step in the pipeline. When it is called without arguments, <paramref name="cancellationToken"/> is passed on.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response to the request.</returns>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
