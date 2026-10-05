using Herald.Tests.Fixtures;
using Herald.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Herald.Tests;

public sealed class PipelineTests
{
    [Fact]
    public async Task Behaviors_RunNestedInRegistrationOrder()
    {
        using var provider = TestHost.Build(configuration => configuration
            .AddBehavior<IPipelineBehavior<Ping, Pong>, OuterPingBehavior>()
            .AddOpenBehavior(typeof(MiddleOpenBehavior<,>))
            .AddBehavior(typeof(IPipelineBehavior<Ping, Pong>), typeof(InnerPingBehavior)));
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new Ping("order"));

        Assert.Equal(
            new[] { "outer:before", "middle:before", "inner:before", "handler:Ping", "inner:after", "middle:after", "outer:after" },
            provider.GetRequiredService<CallLog>().Entries);
    }

    [Fact]
    public async Task Behaviors_ReversedRegistration_RunInReversedOrder()
    {
        using var provider = TestHost.Build(configuration => configuration
            .AddBehavior<IPipelineBehavior<Ping, Pong>, InnerPingBehavior>()
            .AddOpenBehavior(typeof(MiddleOpenBehavior<,>))
            .AddBehavior<IPipelineBehavior<Ping, Pong>, OuterPingBehavior>());
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new Ping("order"));

        Assert.Equal(
            new[] { "inner:before", "middle:before", "outer:before", "handler:Ping", "outer:after", "middle:after", "inner:after" },
            provider.GetRequiredService<CallLog>().Entries);
    }

    [Fact]
    public async Task OpenBehavior_RunsForRequestsWithAndWithoutResponse()
    {
        using var provider = TestHost.Build(configuration => configuration.AddOpenBehavior(typeof(RecordingOpenBehavior<,>)));
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new Ping("open"));
        await mediator.Send(new VoidCommand("open"));
        await mediator.Send((object)new VoidCommand("untyped"));

        Assert.Equal(
            new[]
            {
                "behavior:Ping:Pong", "handler:Ping",
                "behavior:VoidCommand:Unit", "handler:VoidCommand:open",
                "behavior:VoidCommand:Unit", "handler:VoidCommand:untyped",
            },
            provider.GetRequiredService<CallLog>().Entries);
    }
}
