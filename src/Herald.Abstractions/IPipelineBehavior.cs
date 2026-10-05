namespace Herald;

/// <summary>
/// İstek ile handler arasına giren pipeline adımı. Loglama, doğrulama, transaction gibi
/// kesişen işler için kullanılır. Dönüş değeri olmayan istekler bu arayüzde
/// <typeparamref name="TResponse"/> = <see cref="Unit"/> olarak görünür.
/// </summary>
/// <typeparam name="TRequest">İstek tipi.</typeparam>
/// <typeparam name="TResponse">İsteğin dönüş tipi.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : notnull
{
    /// <summary>
    /// İsteği işler. Pipeline'ın devam etmesi için <paramref name="next"/> çağrılmalıdır.
    /// </summary>
    /// <param name="request">İşlenen istek.</param>
    /// <param name="next">
    /// Pipeline'daki bir sonraki adım. Parametresiz çağrılırsa <paramref name="cancellationToken"/> iletilir.
    /// </param>
    /// <param name="cancellationToken">İptal token'ı.</param>
    /// <returns>İsteğin sonucu.</returns>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
