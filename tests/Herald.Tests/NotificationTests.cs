using Herald.Tests.Fixtures;
using Herald.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Herald.Tests;

public sealed class NotificationTests
{
    [Fact]
    public async Task Publish_MultipleHandlers_RunOneAfterAnotherInRegistrationOrder()
    {
        using var provider = TestHost.Build();
        var herald = provider.GetRequiredService<IHerald>();
        var order = GetHandlerNamesInRegistrationOrder(provider);

        await herald.Publish(new OrderPlaced());

        Assert.Equal(
            new[] { $"{order[0]}:start", $"{order[0]}:end", $"{order[1]}:start", $"{order[1]}:end" },
            provider.GetRequiredService<CallLog>().Entries);
    }

    [Fact]
    public async Task Publish_NoHandlers_DoesNothing()
    {
        using var provider = TestHost.Build();
        var herald = provider.GetRequiredService<IHerald>();

        await herald.Publish(new NobodyListens());
        await herald.Publish((object)new NobodyListens());

        Assert.Empty(provider.GetRequiredService<CallLog>().Entries);
    }

    [Fact]
    public async Task Publish_HandlerThrows_ExceptionPropagatesAndLaterHandlersDoNotRun()
    {
        using var provider = TestHost.Build();
        var herald = provider.GetRequiredService<IHerald>();
        var order = GetHandlerNamesInRegistrationOrder(provider);

        var exception = await Assert.ThrowsAsync<TestException>(() => herald.Publish(new OrderPlaced(ThrowFrom: order[0])));

        Assert.Equal(order[0], exception.Message);
        Assert.Equal($"{order[0]}:start", Assert.Single(provider.GetRequiredService<CallLog>().Entries));
    }

    [Fact]
    public async Task PublishObject_Notification_RunsAllHandlers()
    {
        using var provider = TestHost.Build();
        var herald = provider.GetRequiredService<IHerald>();
        object notification = new OrderPlaced();

        await herald.Publish(notification);

        Assert.Equal(4, provider.GetRequiredService<CallLog>().Entries.Count);
    }

    [Fact]
    public async Task PublishObject_NullNotification_ThrowsArgumentNullException()
    {
        using var provider = TestHost.Build();
        var herald = provider.GetRequiredService<IHerald>();

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => herald.Publish((object)null!));

        Assert.Equal("notification", exception.ParamName);
    }

    [Fact]
    public async Task PublishObject_ObjectThatIsNotNotification_ThrowsArgumentException()
    {
        using var provider = TestHost.Build();
        var herald = provider.GetRequiredService<IHerald>();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => herald.Publish((object)new Ping("not a notification")));

        Assert.Equal("notification", exception.ParamName);
        Assert.Contains(typeof(Ping).FullName!, exception.Message);
    }

    [Fact]
    public async Task Publish_NullNotification_ThrowsArgumentNullException()
    {
        using var provider = TestHost.Build();
        var herald = provider.GetRequiredService<IHerald>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => herald.Publish((OrderPlaced)null!));
    }

    private static string[] GetHandlerNamesInRegistrationOrder(IServiceProvider provider)
    {
        var names = provider.GetServices<INotificationHandler<OrderPlaced>>()
            .Select(handler => handler.GetType().Name)
            .ToArray();

        Assert.Equal(2, names.Length);
        return names;
    }
}
