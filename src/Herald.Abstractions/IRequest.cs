namespace Herald;

/// <summary>
/// Marks a request that does not return a value.
/// The class that handles the request implements <see cref="IRequestHandler{TRequest}"/>.
/// </summary>
public interface IRequest : IBaseRequest { }

/// <summary>
/// Marks a request that returns a value of type <typeparamref name="TResponse"/>.
/// The class that handles the request implements <see cref="IRequestHandler{TRequest, TResponse}"/>.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IRequest<out TResponse> : IBaseRequest { }
