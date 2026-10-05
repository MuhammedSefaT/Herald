using Microsoft.Extensions.DependencyInjection;

namespace Herald;

/// <summary>
/// Notification wrapper'larının ortak tabanı.
/// </summary>
internal abstract class NotificationHandlerWrapper
{
    public abstract Task Handle(object notification, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Tüm <see cref="INotificationHandler{TNotification}"/> kayıtlarını DI'dan alır ve kayıt sırasıyla tek tek bekler.
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
