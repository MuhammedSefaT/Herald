using System.Diagnostics;
using Herald;

namespace Herald.Sample.Api.Behaviors;

// Tüm istekler için çalışan açık generic behavior: isteğin adını ve süresini loglar.
// Dönüş değeri olmayan istekler burada TResponse = Unit olarak görünür.
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var startedAt = Stopwatch.GetTimestamp();
        logger.LogInformation("{RequestName} işleniyor", requestName);

        // next() parametresiz çağrıldığında bu behavior'a gelen cancellationToken handler'a iletilir.
        var response = await next();

        logger.LogInformation(
            "{RequestName} {ElapsedMilliseconds} ms içinde tamamlandı",
            requestName,
            Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);

        return response;
    }
}
