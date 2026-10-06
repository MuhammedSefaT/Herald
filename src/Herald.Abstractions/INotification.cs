namespace Herald;

/// <summary>
/// Marks a notification that can be published.
/// A notification can have zero or more <see cref="INotificationHandler{TNotification}"/> handlers.
/// </summary>
public interface INotification { }
