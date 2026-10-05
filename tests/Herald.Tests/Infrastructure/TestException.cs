namespace Herald.Tests.Infrastructure;

/// <summary>
/// Handler'ların fırlattığı ve testlerin aynen geri beklediği exception.
/// </summary>
public sealed class TestException(string message) : Exception(message);
