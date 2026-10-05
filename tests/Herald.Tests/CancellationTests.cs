using Herald.Tests.Fixtures;
using Herald.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Herald.Tests;

public sealed class CancellationTests
{
    [Fact]
    public async Task NextWithoutToken_PassesOuterTokenToHandler()
    {
        using var cancellation = new CancellationTokenSource();
        using var provider = TestHost.Build(configuration => configuration.AddOpenBehavior(typeof(ParameterlessNextBehavior<,>)));
        var mediator = provider.GetRequiredService<IMediator>();
        var recorder = provider.GetRequiredService<TokenRecorder>();

        await mediator.Send(new TokenProbe(), cancellation.Token);

        Assert.Equal(cancellation.Token, recorder.Get("behavior"));
        Assert.Equal(cancellation.Token, recorder.Get("handler"));

        await mediator.Send(new VoidTokenProbe(), cancellation.Token);

        Assert.Equal(cancellation.Token, recorder.Get("behavior"));
        Assert.Equal(cancellation.Token, recorder.Get("handler"));
    }

    [Fact]
    public async Task NextWithToken_PassesThatTokenToInnerLayers()
    {
        using var outer = new CancellationTokenSource();
        using var replacement = new CancellationTokenSource();
        using var provider = TestHost.Build(configuration => configuration
            .AddOpenBehavior(typeof(ParameterlessNextBehavior<,>))
            .AddOpenBehavior(typeof(ReplacingTokenBehavior<,>)));
        var mediator = provider.GetRequiredService<IMediator>();
        var recorder = provider.GetRequiredService<TokenRecorder>();
        recorder.ReplacementToken = replacement.Token;

        await mediator.Send(new TokenProbe(), outer.Token);

        Assert.Equal(outer.Token, recorder.Get("behavior"));
        Assert.Equal(outer.Token, recorder.Get("replacing-behavior"));
        Assert.Equal(replacement.Token, recorder.Get("handler"));
    }

    [Fact]
    public async Task CanceledToken_IsPassedToHandler()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        using var provider = TestHost.Build(configuration => configuration.AddOpenBehavior(typeof(ParameterlessNextBehavior<,>)));
        var mediator = provider.GetRequiredService<IMediator>();
        var recorder = provider.GetRequiredService<TokenRecorder>();

        await mediator.Send(new TokenProbe(), cancellation.Token);

        Assert.True(recorder.Get("handler").IsCancellationRequested);

        await mediator.Send(new VoidTokenProbe(), cancellation.Token);

        Assert.True(recorder.Get("handler").IsCancellationRequested);
    }

    [Fact]
    public async Task CanceledToken_IsPassedToHandlerWithoutBehaviors()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        using var provider = TestHost.Build();
        var mediator = provider.GetRequiredService<IMediator>();
        var recorder = provider.GetRequiredService<TokenRecorder>();

        await mediator.Send((object)new TokenProbe(), cancellation.Token);

        Assert.Equal(cancellation.Token, recorder.Get("handler"));
        Assert.True(recorder.Get("handler").IsCancellationRequested);
    }
}
