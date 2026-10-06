namespace Herald.Tests.Infrastructure;

/// <summary>
/// Exception thrown by test handlers; tests expect to receive it unchanged.
/// </summary>
public sealed class TestException(string message) : Exception(message);
