namespace Herald;

/// <summary>
/// Common marker interface for all requests.
/// Do not implement it directly; implement <see cref="IRequest"/> or <see cref="IRequest{TResponse}"/> instead.
/// </summary>
public interface IBaseRequest { }
