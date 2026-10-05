namespace Herald;

/// <summary>
/// Bir bildirimi işleyen handler. Aynı bildirim için birden fazla handler bulunabilir.
/// </summary>
/// <typeparam name="TNotification">İşlenen bildirim tipi.</typeparam>
public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    /// <summary>
    /// Bildirimi işler.
    /// </summary>
    /// <param name="notification">İşlenecek bildirim.</param>
    /// <param name="cancellationToken">İptal token'ı.</param>
    /// <returns>İşlemin tamamlanmasını temsil eden task.</returns>
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
