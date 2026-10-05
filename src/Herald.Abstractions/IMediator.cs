namespace Herald;

/// <summary>
/// İstek gönderme (<see cref="ISender"/>) ve bildirim yayınlama (<see cref="IPublisher"/>) işlemlerini bir arada sunar.
/// </summary>
public interface IMediator : ISender, IPublisher { }
