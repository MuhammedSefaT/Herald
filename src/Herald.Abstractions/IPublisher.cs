namespace Herald;

/// <summary>
/// Bildirimleri kayıtlı tüm handler'lara yayınlar.
/// </summary>
public interface IPublisher
{
    /// <summary>
    /// Bildirimi kayıtlı tüm handler'lara sırayla iletir. Handler yoksa hiçbir şey yapmaz.
    /// Bir handler exception fırlatırsa exception yukarı iletilir ve sonraki handler'lar çalışmaz.
    /// </summary>
    /// <typeparam name="TNotification">Bildirim tipi.</typeparam>
    /// <param name="notification">Yayınlanacak bildirim.</param>
    /// <param name="cancellationToken">İptal token'ı.</param>
    /// <returns>Tüm handler'ların tamamlanmasını temsil eden task.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="notification"/> null ise.</exception>
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;

    /// <summary>
    /// Tipi derleme zamanında bilinmeyen bir bildirimi kayıtlı tüm handler'lara sırayla iletir.
    /// Handler yoksa hiçbir şey yapmaz.
    /// </summary>
    /// <param name="notification">Yayınlanacak bildirim. <see cref="INotification"/> uygulamalıdır.</param>
    /// <param name="cancellationToken">İptal token'ı.</param>
    /// <returns>Tüm handler'ların tamamlanmasını temsil eden task.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="notification"/> null ise.</exception>
    /// <exception cref="ArgumentException"><paramref name="notification"/>, <see cref="INotification"/> uygulamıyorsa.</exception>
    Task Publish(object notification, CancellationToken cancellationToken = default);
}
