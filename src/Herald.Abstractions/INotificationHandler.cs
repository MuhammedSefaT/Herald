namespace Herald;

/// <summary>
/// Handles a notification. A notification can have more than one handler.
/// </summary>
/// <typeparam name="TNotification">The type of notification being handled.</typeparam>
public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    /// <summary>
    /// Handles the notification.
    /// </summary>
    /// <param name="notification">The notification to handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the operation.</returns>
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
