namespace Herald;

/// <summary>
/// Pipeline'daki bir sonraki adımı temsil eder: bir sonraki behavior ya da en içte isteğin handler'ı.
/// </summary>
/// <param name="cancellationToken">
/// Bir sonraki adıma iletilecek iptal token'ı. Verilmezse behavior'ın aldığı token iletilir.
/// </param>
/// <typeparam name="TResponse">İsteğin dönüş tipi.</typeparam>
/// <returns>İsteğin sonucu.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);
