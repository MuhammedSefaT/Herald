using Herald.Tests.Fixtures;
using Herald.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Herald.Tests;

public sealed class CacheTests
{
    [Fact]
    public async Task Send_SameRequestTypeRepeatedly_ReturnsCorrectResults()
    {
        using var provider = TestHost.Build();
        var herald = provider.GetRequiredService<IHerald>();

        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(new Pong($"{i} pong"), await herald.Send(new Ping($"{i}")));
            Assert.Equal(new Pong($"{i} pong"), await herald.Send((object)new Ping($"{i}")));
            await herald.Send(new VoidCommand($"{i}"));
        }

        Assert.Equal(5, provider.GetRequiredService<CallLog>().Entries.Count(entry => entry.StartsWith("handler:VoidCommand:", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task ParallelCalls_OnColdCache_AllReturnCorrectResults()
    {
        const int callCount = 400;
        using var provider = TestHost.Build();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var calls = Enumerable.Range(0, callCount)
            .Select(value => Task.Run(async () =>
            {
                await start.Task;
                var herald = provider.GetRequiredService<IHerald>();

                switch (value % 4)
                {
                    case 0:
                        Assert.Equal(value * 2, await herald.Send(new ParallelRequest(value)));
                        break;
                    case 1:
                        Assert.Equal(value * 2, await herald.Send((object)new ParallelRequest(value)));
                        break;
                    case 2:
                        await herald.Send(new ParallelVoidRequest(value));
                        break;
                    default:
                        await herald.Publish(new ParallelNotification(value));
                        break;
                }
            }))
            .ToArray();

        start.SetResult();
        await Task.WhenAll(calls);

        var entries = provider.GetRequiredService<CallLog>().Entries;
        Assert.Equal(callCount / 4, entries.Count(entry => entry.StartsWith("handler:ParallelVoidRequest:", StringComparison.Ordinal)));
        Assert.Equal(callCount / 4, entries.Count(entry => entry.StartsWith("handler:ParallelNotification:", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task CachedWrapper_DoesNotShareBehaviorsBetweenServiceProviders()
    {
        using var withBehavior = TestHost.Build(configuration => configuration.AddOpenBehavior(typeof(RecordingOpenBehavior<,>)));
        using var withoutBehavior = TestHost.Build();

        Assert.Equal("isolated", await withBehavior.GetRequiredService<IHerald>().Send(new CacheIsolationRequest()));
        Assert.Equal("isolated", await withoutBehavior.GetRequiredService<IHerald>().Send(new CacheIsolationRequest()));

        Assert.Equal("behavior:CacheIsolationRequest:String", Assert.Single(withBehavior.GetRequiredService<CallLog>().Entries));
        Assert.Empty(withoutBehavior.GetRequiredService<CallLog>().Entries);
    }
}
