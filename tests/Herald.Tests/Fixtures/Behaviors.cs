using Herald.Tests.Infrastructure;

namespace Herald.Tests.Fixtures;

// Closed behaviors for Ping, used by the ordering tests.
public sealed class OuterPingBehavior(CallLog log) : IPipelineBehavior<Ping, Pong>
{
    public async Task<Pong> Handle(Ping request, RequestHandlerDelegate<Pong> next, CancellationToken cancellationToken)
    {
        log.Add("outer:before");
        var response = await next();
        log.Add("outer:after");
        return response;
    }
}

public sealed class InnerPingBehavior(CallLog log) : IPipelineBehavior<Ping, Pong>
{
    public async Task<Pong> Handle(Ping request, RequestHandlerDelegate<Pong> next, CancellationToken cancellationToken)
    {
        log.Add("inner:before");
        var response = await next();
        log.Add("inner:after");
        return response;
    }
}

// Open generic behaviors that run for every request.
public sealed class MiddleOpenBehavior<TRequest, TResponse>(CallLog log) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        log.Add("middle:before");
        var response = await next();
        log.Add("middle:after");
        return response;
    }
}

public sealed class RecordingOpenBehavior<TRequest, TResponse>(CallLog log) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        log.Add($"behavior:{typeof(TRequest).Name}:{typeof(TResponse).Name}");
        return next();
    }
}

// Behaviors for the cancellation token tests.
public sealed class ParameterlessNextBehavior<TRequest, TResponse>(TokenRecorder recorder) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        recorder.Record("behavior", cancellationToken);
        return next();
    }
}

public sealed class ReplacingTokenBehavior<TRequest, TResponse>(TokenRecorder recorder) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        recorder.Record("replacing-behavior", cancellationToken);
        return next(recorder.ReplacementToken);
    }
}
