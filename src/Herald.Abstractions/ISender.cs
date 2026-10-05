namespace Herald;

/// <summary>
/// İstekleri pipeline behavior'lardan geçirerek tek bir handler'a gönderir.
/// </summary>
public interface ISender
{
    /// <summary>
    /// Dönüş değeri olan bir isteği gönderir.
    /// </summary>
    /// <typeparam name="TResponse">İsteğin dönüş tipi.</typeparam>
    /// <param name="request">Gönderilecek istek.</param>
    /// <param name="cancellationToken">İptal token'ı.</param>
    /// <returns>Handler'ın döndürdüğü sonuç.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> null ise.</exception>
    /// <exception cref="InvalidOperationException">İstek için kayıtlı bir handler yoksa.</exception>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dönüş değeri olmayan bir isteği gönderir.
    /// </summary>
    /// <typeparam name="TRequest">İstek tipi.</typeparam>
    /// <param name="request">Gönderilecek istek.</param>
    /// <param name="cancellationToken">İptal token'ı.</param>
    /// <returns>İşlemin tamamlanmasını temsil eden task.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> null ise.</exception>
    /// <exception cref="InvalidOperationException">İstek için kayıtlı bir handler yoksa.</exception>
    Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest;

    /// <summary>
    /// Tipi derleme zamanında bilinmeyen bir isteği gönderir.
    /// </summary>
    /// <param name="request">
    /// Gönderilecek istek. <see cref="IRequest"/> veya <see cref="IRequest{TResponse}"/> uygulamalıdır.
    /// </param>
    /// <param name="cancellationToken">İptal token'ı.</param>
    /// <returns>Handler'ın döndürdüğü sonuç; dönüş değeri olmayan isteklerde <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> null ise.</exception>
    /// <exception cref="ArgumentException"><paramref name="request"/>, <see cref="IBaseRequest"/> uygulamıyorsa.</exception>
    /// <exception cref="InvalidOperationException">İstek için kayıtlı bir handler yoksa.</exception>
    Task<object?> Send(object request, CancellationToken cancellationToken = default);
}
