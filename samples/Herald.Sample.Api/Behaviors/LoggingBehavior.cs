using System.Diagnostics;
using Herald;

namespace Herald.Sample.Api.Behaviors;

// Open generic behavior that runs for every request: logs the request name and duration.
// Requests that do not return a value appear here with TResponse = Unit.
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var startedAt = Stopwatch.GetTimestamp();
        logger.LogInformation("Handling {RequestName}", requestName);

        // When next() is called without arguments, the cancellationToken this behavior received is passed to the handler.
        var response = await next();

        logger.LogInformation(
            "Handled {RequestName} in {ElapsedMilliseconds} ms",
            requestName,
            Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);

        return response;
    }
}
