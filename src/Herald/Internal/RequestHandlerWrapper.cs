using Microsoft.Extensions.DependencyInjection;

namespace Herald;

/// <summary>
/// Common base of the request wrappers. Requests whose type is not known at compile time are sent through this type.
/// </summary>
internal abstract class RequestHandlerBase
{
    /// <summary>
    /// Handles the request and returns the response as <see cref="object"/>; returns <see langword="null"/> for requests without a response.
    /// </summary>
    public abstract Task<object?> HandleUntyped(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);

    /// <summary>
    /// Runs the registered behaviors nested in registration order: the first registered behavior is outermost, the handler is innermost.
    /// </summary>
    protected static Task<TResponse> RunPipeline<TRequest, TResponse>(
        TRequest request,
        IServiceProvider serviceProvider,
        Func<CancellationToken, Task<TResponse>> handler,
        CancellationToken cancellationToken)
        where TRequest : notnull
    {
        var services = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>();
        var behaviors = services as IPipelineBehavior<TRequest, TResponse>[] ?? services.ToArray();

        return Next(0, cancellationToken);

        Task<TResponse> Next(int index, CancellationToken token)
        {
            if (index == behaviors.Length)
            {
                return handler(token);
            }

            // When next() is called without a token, the default token arrives; the token this behavior received is passed on instead.
            return behaviors[index].Handle(
                request,
                nextToken => Next(index + 1, nextToken.CanBeCanceled ? nextToken : token),
                token);
        }
    }

    protected static InvalidOperationException HandlerNotFound(Type requestType) =>
        new($"No handler is registered for request '{requestType.FullName}'. " +
            "Was the assembly that contains the handler registered with AddHerald?");
}

/// <summary>
/// Type-safe entry point for requests that return a value.
/// </summary>
internal abstract class RequestHandlerWrapper<TResponse> : RequestHandlerBase
{
    public abstract Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Resolves the <see cref="IRequestHandler{TRequest, TResponse}"/> handler from DI and runs it inside the pipeline.
/// </summary>
internal sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    public override async Task<object?> HandleUntyped(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken) =>
        await Handle((IRequest<TResponse>)request, serviceProvider, cancellationToken).ConfigureAwait(false);

    public override Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>()
            ?? throw HandlerNotFound(typeof(TRequest));
        var typedRequest = (TRequest)request;

        return RunPipeline(typedRequest, serviceProvider, token => handler.Handle(typedRequest, token), cancellationToken);
    }
}
