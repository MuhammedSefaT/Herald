using System.Collections.Concurrent;

namespace Herald;

/// <summary>
/// Default implementation of <see cref="IHerald"/>. Resolves handlers and behaviors from the
/// <see cref="IServiceProvider"/> passed to the constructor.
/// </summary>
/// <remarks>
/// The wrapper for each request and notification type is created once on first use and
/// reused from a cache afterwards; no reflection runs per call.
/// </remarks>
internal sealed class HeraldDispatcher : IHerald
{
    private static readonly ConcurrentDictionary<Type, RequestHandlerBase> RequestHandlers = new();
    private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> NotificationHandlers = new();

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Creates a new <see cref="HeraldDispatcher"/>.
    /// </summary>
    /// <param name="serviceProvider">The service provider that resolves handlers and behaviors.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is null.</exception>
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

        // IRequest<out TResponse> is covariant, so TResponse can be a base type of the request's real
        // response type (for example, an IRequest<string> request sent as IRequest<object>).
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
