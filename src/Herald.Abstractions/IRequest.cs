namespace Herald;

/// <summary>
/// Dönüş değeri olmayan bir isteği işaretler.
/// İsteği işleyen sınıf <see cref="IRequestHandler{TRequest}"/> arayüzünü uygular.
/// </summary>
public interface IRequest : IBaseRequest { }

/// <summary>
/// <typeparamref name="TResponse"/> tipinde değer döndüren bir isteği işaretler.
/// İsteği işleyen sınıf <see cref="IRequestHandler{TRequest, TResponse}"/> arayüzünü uygular.
/// </summary>
/// <typeparam name="TResponse">İsteğin dönüş tipi.</typeparam>
public interface IRequest<out TResponse> : IBaseRequest { }
