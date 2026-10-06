using Microsoft.Extensions.DependencyInjection;

namespace Herald;

/// <summary>
/// Type-safe entry point for requests that do not return a value.
/// </summary>
internal abstract class VoidRequestHandlerWrapper : RequestHandlerBase
{
    public abstract Task Handle(IRequest request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Resolves the <see cref="IRequestHandler{TRequest}"/> handler from DI and runs it inside the pipeline.
/// Inside the pipeline the response type is <see cref="Unit"/>, so these requests also pass through
/// <see cref="IPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
internal sealed class VoidRequestHandlerWrapper<TRequest> : VoidRequestHandlerWrapper
    where TRequest : IRequest
{
    public override async Task<object?> HandleUntyped(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        await Handle((IRequest)request, serviceProvider, cancellationToken).ConfigureAwait(false);
        return null;
    }

    public override Task Handle(IRequest request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetService<IRequestHandler<TRequest>>()
            ?? throw HandlerNotFound(typeof(TRequest));
        var typedRequest = (TRequest)request;

        return RunPipeline(typedRequest, serviceProvider, token => InvokeHandler(handler, typedRequest, token), cancellationToken);
    }

    private static async Task<Unit> InvokeHandler(IRequestHandler<TRequest> handler, TRequest request, CancellationToken cancellationToken)
    {
        await handler.Handle(request, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
