namespace Herald;

/// <summary>
/// Tüm istekler için ortak işaretleyici arayüz.
/// Doğrudan uygulanmaz; bunun yerine <see cref="IRequest"/> veya <see cref="IRequest{TResponse}"/> kullanılır.
/// </summary>
public interface IBaseRequest { }
