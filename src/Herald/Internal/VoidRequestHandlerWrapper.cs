using Microsoft.Extensions.DependencyInjection;

namespace Herald;

/// <summary>
/// Dönüş değeri olmayan istekler için tip güvenli giriş noktası.
/// </summary>
internal abstract class VoidRequestHandlerWrapper : RequestHandlerBase
{
    public abstract Task Handle(IRequest request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// <see cref="IRequestHandler{TRequest}"/> handler'ını DI'dan alıp pipeline içinde çalıştırır.
/// Pipeline içinde dönüş tipi <see cref="Unit"/> olur; böylece bu istekler de
/// <see cref="IPipelineBehavior{TRequest, TResponse}"/> ile yakalanır.
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
