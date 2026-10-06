using Herald.Tests.Fixtures;
using Herald.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Herald.Tests;

public sealed class RequestTests
{
    [Fact]
    public async Task Send_RequestWithResponse_ReturnsHandlerResult()
    {
        using var provider = TestHost.Build();
        var herald = provider.GetRequiredService<IHerald>();

        var response = await herald.Send(new Ping("hello"));

        Assert.Equal(new Pong("hello pong"), response);
        Assert.Equal("handler:Ping", Assert.Single(provider.GetRequiredService<CallLog>().Entries));
    }

    [Fact]
    public async Task Send_RequestWithoutResponse_InvokesHandler()
    {
        using var provider = TestHost.Build();
        var herald = provider.GetRequiredService<IHerald>();

        await herald.Send(new VoidCommand("run"));

        Assert.Equal("handler:VoidCommand:run", Assert.Single(provider.GetRequiredService<CallLog>().Entries));
    }

    [Fact]
    public async Task SendObject_RequestWithResponse_ReturnsHandlerResult()
    {
        using var provider = TestHost.Build();
        var herald = provider.GetRequiredService<IHerald>();
        object request = new Ping("boxed");

        var response = await herald.Send(request);

        Assert.Equal(new Pong("boxed pong"), response);
    }

    [Fact]
    public async Task SendObject_RequestWithoutResponse_InvokesHandlerAndReturnsNull()
    {
        using var provider = TestHost.Build();
        var herald = provider.GetRequiredService<IHerald>();
        object request = new VoidCommand("boxed");

        var response = await herald.Send(request);

        Assert.Null(response);
        Assert.Equal("handler:VoidCommand:boxed", Assert.Single(provider.GetRequiredService<CallLog>().Entries));
    }

    [Fact]
    public async Task SendObject_NullRequest_ThrowsArgumentNullException()
    {
        using var provider = TestHost.Build();
        var herald = provider.GetRequiredService<IHerald>();

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => herald.Send((object)null!));

        Assert.Equal("request", exception.ParamName);
    }

    [Fact]
    public async Task SendObject_ObjectThatIsNotRequest_ThrowsArgumentException()
    {
        using var provider = TestHost.Build();
        var herald = provider.GetRequiredService<IHerald>();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => herald.Send((object)"not a request"));

        Assert.Equal("request", exception.ParamName);
        Assert.Contains(typeof(string).FullName!, exception.Message);
    }

    [Fact]
    public async Task Send_NullRequest_ThrowsArgumentNullException()
    {
        using var provider = TestHost.Build();
        var herald = provider.GetRequiredService<IHerald>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => herald.Send((IRequest<Pong>)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => herald.Send((VoidCommand)null!));
    }

    [Fact]
    public async Task Send_RequestWithResponseWithoutHandler_ThrowsInvalidOperationException()
    {
        using var provider = TestHost.Build();
        var herald = provider.GetRequiredService<IHerald>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => herald.Send(new UnhandledRequest()));

        Assert.Contains(typeof(UnhandledRequest).FullName!, exception.Message);
        Assert.Contains("AddHerald", exception.Message);
    }

    [Fact]
    public async Task Send_RequestWithoutResponseWithoutHandler_ThrowsInvalidOperationException()
    {
        using var provider = TestHost.Build();
        var herald = provider.GetRequiredService<IHerald>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => herald.Send(new UnhandledVoidRequest()));

        Assert.Contains(typeof(UnhandledVoidRequest).FullName!, exception.Message);
        Assert.Contains("AddHerald", exception.Message);
    }

    [Fact]
    public async Task SendObject_RequestWithoutHandler_ThrowsInvalidOperationException()
    {
        using var provider = TestHost.Build();
        var herald = provider.GetRequiredService<IHerald>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => herald.Send((object)new UnhandledRequest()));

        Assert.Contains(typeof(UnhandledRequest).FullName!, exception.Message);
    }

    [Fact]
    public async Task Send_HandlerThrows_ExceptionPropagatesUnwrapped()
    {
        using var provider = TestHost.Build();
        var herald = provider.GetRequiredService<IHerald>();

        var withResponse = await Assert.ThrowsAsync<TestException>(() => herald.Send(new ThrowingRequest()));
        var withoutResponse = await Assert.ThrowsAsync<TestException>(() => herald.Send(new ThrowingVoidRequest()));
        var untyped = await Assert.ThrowsAsync<TestException>(() => herald.Send((object)new ThrowingRequest()));

        Assert.Equal("ThrowingRequestHandler", withResponse.Message);
        Assert.Equal("ThrowingVoidRequestHandler", withoutResponse.Message);
        Assert.Equal("ThrowingRequestHandler", untyped.Message);
    }

    [Fact]
    public async Task Send_RequestThroughCovariantResponseType_ReturnsHandlerResult()
    {
        using var provider = TestHost.Build();
        var herald = provider.GetRequiredService<IHerald>();
        IRequest<object> request = new Ping("covariant");

        var response = await herald.Send(request);

        Assert.Equal(new Pong("covariant pong"), response);
        Assert.Equal(new Pong("typed pong"), await herald.Send(new Ping("typed")));
    }

    [Fact]
    public async Task SendObject_RequestImplementingBothRequestKinds_ThrowsInvalidOperationException()
    {
        using var provider = TestHost.Build();
        var herald = provider.GetRequiredService<IHerald>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => herald.Send((object)new AmbiguousRequest()));

        Assert.Contains(typeof(AmbiguousRequest).FullName!, exception.Message);
    }
}
