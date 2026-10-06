namespace Herald;

/// <summary>
/// Publishes notifications to all registered handlers.
/// </summary>
public interface IPublisher
{
    /// <summary>
    /// Publishes the notification to all registered handlers, one after another. Does nothing when there is no handler.
    /// If a handler throws, the exception is propagated and the remaining handlers do not run.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification.</typeparam>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the completion of all handlers.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="notification"/> is null.</exception>
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;

    /// <summary>
    /// Publishes a notification whose type is not known at compile time to all registered handlers, one after another.
    /// Does nothing when there is no handler.
    /// </summary>
    /// <param name="notification">The notification to publish. It must implement <see cref="INotification"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the completion of all handlers.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="notification"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="notification"/> does not implement <see cref="INotification"/>.</exception>
    Task Publish(object notification, CancellationToken cancellationToken = default);
}
