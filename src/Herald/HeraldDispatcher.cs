using System.Collections.Concurrent;

namespace Herald;

/// <summary>
/// <see cref="IHerald"/> arayüzünün varsayılan uygulaması. Handler ve behavior'ları
/// constructor'da verilen <see cref="IServiceProvider"/> üzerinden çözümler.
/// </summary>
/// <remarks>
/// Her istek ve bildirim tipi için gerekli wrapper ilk kullanımda bir kez oluşturulur ve
/// sonraki çağrılarda önbellekten kullanılır; çağrı başına reflection yapılmaz.
/// </remarks>
public sealed class HeraldDispatcher : IHerald
{
    private static readonly ConcurrentDictionary<Type, RequestHandlerBase> RequestHandlers = new();
    private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> NotificationHandlers = new();

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Yeni bir <see cref="HeraldDispatcher"/> örneği oluşturur.
    /// </summary>
    /// <param name="serviceProvider">Handler ve behavior'ların çözümleneceği servis sağlayıcı.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> null ise.</exception>
    public HeraldDispatcher(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = GetRequestHandler(request.GetType());

        // IRequest<out TResponse> kovaryant olduğu için TResponse, isteğin gerçek dönüş tipinin
        // bir üst tipi olabilir (ör. IRequest<string> isteğinin IRequest<object> olarak gönderilmesi).
        return wrapper is RequestHandlerWrapper<TResponse> typedWrapper
            ? typedWrapper.Handle(request, _serviceProvider, cancellationToken)
            : SendAsBaseResponse<TResponse>(wrapper, request, cancellationToken);
    }

    /// <inheritdoc />
    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = (VoidRequestHandlerWrapper)GetRequestHandler(request.GetType());
        return wrapper.Handle(request, _serviceProvider, cancellationToken);
    }

    /// <inheritdoc />
    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request is not IBaseRequest)
        {
            throw new ArgumentException(
                $"Type '{request.GetType().FullName}' does not implement {nameof(IBaseRequest)}. " +
                "A request must implement IRequest or IRequest<TResponse>.",
                nameof(request));
        }

        return GetRequestHandler(request.GetType()).HandleUntyped(request, _serviceProvider, cancellationToken);
    }

    /// <inheritdoc />
    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        return GetNotificationHandler(notification.GetType()).Handle(notification, _serviceProvider, cancellationToken);
    }

    /// <inheritdoc />
    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (notification is not INotification)
        {
            throw new ArgumentException(
                $"Type '{notification.GetType().FullName}' does not implement {nameof(INotification)}.",
                nameof(notification));
        }

        return GetNotificationHandler(notification.GetType()).Handle(notification, _serviceProvider, cancellationToken);
    }

    private async Task<TResponse> SendAsBaseResponse<TResponse>(RequestHandlerBase wrapper, object request, CancellationToken cancellationToken) =>
        (TResponse)(await wrapper.HandleUntyped(request, _serviceProvider, cancellationToken).ConfigureAwait(false))!;

    private static RequestHandlerBase GetRequestHandler(Type requestType) =>
        RequestHandlers.GetOrAdd(requestType, CreateRequestHandler);

    private static NotificationHandlerWrapper GetNotificationHandler(Type notificationType) =>
        NotificationHandlers.GetOrAdd(notificationType, CreateNotificationHandler);

    private static RequestHandlerBase CreateRequestHandler(Type requestType)
    {
        var isVoidRequest = typeof(IRequest).IsAssignableFrom(requestType);
        var responseTypes = requestType.GetInterfaces()
            .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRequest<>))
            .Select(type => type.GetGenericArguments()[0])
            .ToArray();

        var wrapperType = (isVoidRequest, responseTypes.Length) switch
        {
            (true, 0) => typeof(VoidRequestHandlerWrapper<>).MakeGenericType(requestType),
            (false, 1) => typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, responseTypes[0]),
            _ => throw new InvalidOperationException(
                $"Request type '{requestType.FullName}' must implement exactly one of IRequest or IRequest<TResponse>."),
        };

        return (RequestHandlerBase)Activator.CreateInstance(wrapperType)!;
    }

    private static NotificationHandlerWrapper CreateNotificationHandler(Type notificationType)
    {
        var wrapperType = typeof(NotificationHandlerWrapper<>).MakeGenericType(notificationType);
        return (NotificationHandlerWrapper)Activator.CreateInstance(wrapperType)!;
    }
}
