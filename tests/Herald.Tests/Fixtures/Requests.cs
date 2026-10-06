using Herald.Tests.Infrastructure;

namespace Herald.Tests.Fixtures;

// Request that returns a value.
public sealed record Ping(string Message) : IRequest<Pong>;

public sealed record Pong(string Message);

public sealed class PingHandler(CallLog log) : IRequestHandler<Ping, Pong>
{
    public Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
    {
        log.Add("handler:Ping");
        return Task.FromResult(new Pong(request.Message + " pong"));
    }
}

// Request that does not return a value.
public sealed record VoidCommand(string Name) : IRequest;

public sealed class VoidCommandHandler(CallLog log) : IRequestHandler<VoidCommand>
{
    public Task Handle(VoidCommand request, CancellationToken cancellationToken)
    {
        log.Add($"handler:VoidCommand:{request.Name}");
        return Task.CompletedTask;
    }
}

// Requests without handlers.
public sealed record UnhandledRequest : IRequest<string>;

public sealed record UnhandledVoidRequest : IRequest;

// Request that implements both IRequest and IRequest<TResponse> and therefore cannot be sent.
public sealed record AmbiguousRequest : IRequest, IRequest<string>;

// Request whose handlers are generated in a dynamic assembly during the test.
public sealed record ConflictRequest : IRequest<string>;

// Requests whose handlers throw.
public sealed record ThrowingRequest : IRequest<string>;

public sealed class ThrowingRequestHandler : IRequestHandler<ThrowingRequest, string>
{
    public async Task<string> Handle(ThrowingRequest request, CancellationToken cancellationToken)
    {
        await Task.Yield();
        throw new TestException("ThrowingRequestHandler");
    }
}

public sealed record ThrowingVoidRequest : IRequest;

public sealed class ThrowingVoidRequestHandler : IRequestHandler<ThrowingVoidRequest>
{
    public async Task Handle(ThrowingVoidRequest request, CancellationToken cancellationToken)
    {
        await Task.Yield();
        throw new TestException("ThrowingVoidRequestHandler");
    }
}

// Requests whose handlers record the token they receive.
public sealed record TokenProbe : IRequest<string>;

public sealed class TokenProbeHandler(TokenRecorder recorder) : IRequestHandler<TokenProbe, string>
{
    public Task<string> Handle(TokenProbe request, CancellationToken cancellationToken)
    {
        recorder.Record("handler", cancellationToken);
        return Task.FromResult("ok");
    }
}

public sealed record VoidTokenProbe : IRequest;

public sealed class VoidTokenProbeHandler(TokenRecorder recorder) : IRequestHandler<VoidTokenProbe>
{
    public Task Handle(VoidTokenProbe request, CancellationToken cancellationToken)
    {
        recorder.Record("handler", cancellationToken);
        return Task.CompletedTask;
    }
}

// A single class that implements several handler interfaces.
public sealed record MultiRequest : IRequest<string>;

public sealed record MultiVoidRequest : IRequest;

public sealed record MultiNotification : INotification;

public sealed class MultiHandler(CallLog log) :
    IRequestHandler<MultiRequest, string>,
    IRequestHandler<MultiVoidRequest>,
    INotificationHandler<MultiNotification>
{
    public Task<string> Handle(MultiRequest request, CancellationToken cancellationToken) =>
        Task.FromResult("multi");

    public Task Handle(MultiVoidRequest request, CancellationToken cancellationToken)
    {
        log.Add("handler:MultiVoidRequest");
        return Task.CompletedTask;
    }

    public Task Handle(MultiNotification notification, CancellationToken cancellationToken)
    {
        log.Add("handler:MultiNotification");
        return Task.CompletedTask;
    }
}

// An open generic handler must not be scanned.
public sealed record GenericRequest<T>(T Value) : IRequest<string>;

public sealed class GenericRequestHandler<T> : IRequestHandler<GenericRequest<T>, string>
{
    public Task<string> Handle(GenericRequest<T> request, CancellationToken cancellationToken) =>
        Task.FromResult($"{request.Value}");
}

// An abstract handler must not be scanned; the concrete handler derived from it must be.
public sealed record InheritedRequest : IRequest<string>;

public abstract class InheritedRequestHandlerBase : IRequestHandler<InheritedRequest, string>
{
    public abstract Task<string> Handle(InheritedRequest request, CancellationToken cancellationToken);
}

public sealed class InheritedRequestHandler : InheritedRequestHandlerBase
{
    public override Task<string> Handle(InheritedRequest request, CancellationToken cancellationToken) =>
        Task.FromResult("derived");
}

// Requests used only by the cache tests.
public sealed record ParallelRequest(int Value) : IRequest<int>;

public sealed class ParallelRequestHandler : IRequestHandler<ParallelRequest, int>
{
    public async Task<int> Handle(ParallelRequest request, CancellationToken cancellationToken)
    {
        await Task.Yield();
        return request.Value * 2;
    }
}

public sealed record ParallelVoidRequest(int Value) : IRequest;

public sealed class ParallelVoidRequestHandler(CallLog log) : IRequestHandler<ParallelVoidRequest>
{
    public async Task Handle(ParallelVoidRequest request, CancellationToken cancellationToken)
    {
        await Task.Yield();
        log.Add($"handler:ParallelVoidRequest:{request.Value}");
    }
}

public sealed record CacheIsolationRequest : IRequest<string>;

public sealed class CacheIsolationRequestHandler : IRequestHandler<CacheIsolationRequest, string>
{
    public Task<string> Handle(CacheIsolationRequest request, CancellationToken cancellationToken) =>
        Task.FromResult("isolated");
}
