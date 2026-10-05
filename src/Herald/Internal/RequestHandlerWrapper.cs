using Microsoft.Extensions.DependencyInjection;

namespace Herald;

/// <summary>
/// Request wrapper'larının ortak tabanı. Tipi derleme zamanında bilinmeyen istekler bu tip üzerinden gönderilir.
/// </summary>
internal abstract class RequestHandlerBase
{
    /// <summary>
    /// İsteği işler ve sonucu <see cref="object"/> olarak döndürür; dönüşü olmayan isteklerde <see langword="null"/> döner.
    /// </summary>
    public abstract Task<object?> HandleUntyped(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);

    /// <summary>
    /// Kayıtlı behavior'ları kayıt sırasına göre iç içe çalıştırır: ilk kaydedilen en dışta, handler en içte.
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

            // next() parametresiz çağrılırsa default token gelir; bu durumda behavior'ın aldığı token iletilir.
            return behaviors[index].Handle(
                request,
                nextToken => Next(index + 1, nextToken.CanBeCanceled ? nextToken : token),
                token);
        }
    }

    protected static InvalidOperationException HandlerNotFound(Type requestType) =>
        new($"'{requestType.FullName}' isteği için kayıtlı bir handler bulunamadı. " +
            "Handler'ın bulunduğu assembly AddHerald ile kaydedildi mi?");
}

/// <summary>
/// Dönüş değeri olan istekler için tip güvenli giriş noktası.
/// </summary>
internal abstract class RequestHandlerWrapper<TResponse> : RequestHandlerBase
{
    public abstract Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// <see cref="IRequestHandler{TRequest, TResponse}"/> handler'ını DI'dan alıp pipeline içinde çalıştırır.
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
