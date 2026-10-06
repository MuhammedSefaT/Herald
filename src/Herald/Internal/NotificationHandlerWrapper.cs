using Microsoft.Extensions.DependencyInjection;

namespace Herald;

/// <summary>
/// Common base of the notification wrappers.
/// </summary>
internal abstract class NotificationHandlerWrapper
{
    public abstract Task Handle(object notification, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Resolves all <see cref="INotificationHandler{TNotification}"/> registrations from DI and awaits them one by one in registration order.
/// </summary>
internal sealed class NotificationHandlerWrapper<TNotification> : NotificationHandlerWrapper
    where TNotification : INotification
{
    public override async Task Handle(object notification, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var typedNotification = (TNotification)notification;

        foreach (var handler in serviceProvider.GetServices<INotificationHandler<TNotification>>())
        {
            await handler.Handle(typedNotification, cancellationToken).ConfigureAwait(false);
        }
    }
}
