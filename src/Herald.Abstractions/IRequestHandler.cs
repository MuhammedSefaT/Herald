namespace Herald;

/// <summary>
/// Dönüş değeri olan bir isteği işleyen handler.
/// Her istek tipi için tam olarak bir handler bulunmalıdır.
/// </summary>
/// <typeparam name="TRequest">İşlenen istek tipi.</typeparam>
/// <typeparam name="TResponse">İsteğin dönüş tipi.</typeparam>
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// İsteği işler ve sonucu döndürür.
    /// </summary>
    /// <param name="request">İşlenecek istek.</param>
    /// <param name="cancellationToken">İptal token'ı.</param>
    /// <returns>İsteğin sonucu.</returns>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Dönüş değeri olmayan bir isteği işleyen handler.
/// Her istek tipi için tam olarak bir handler bulunmalıdır.
/// </summary>
/// <typeparam name="TRequest">İşlenen istek tipi.</typeparam>
public interface IRequestHandler<in TRequest> where TRequest : IRequest
{
    /// <summary>
    /// İsteği işler.
    /// </summary>
    /// <param name="request">İşlenecek istek.</param>
    /// <param name="cancellationToken">İptal token'ı.</param>
    /// <returns>İşlemin tamamlanmasını temsil eden task.</returns>
    Task Handle(TRequest request, CancellationToken cancellationToken);
}
