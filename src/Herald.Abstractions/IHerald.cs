namespace Herald;

/// <summary>
/// Combines sending requests (<see cref="ISender"/>) and publishing notifications (<see cref="IPublisher"/>).
/// </summary>
public interface IHerald : ISender, IPublisher { }
