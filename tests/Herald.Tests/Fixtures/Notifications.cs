using Herald.Tests.Infrastructure;

namespace Herald.Tests.Fixtures;

// Notification with two handlers. ThrowFrom is the name of the handler that should throw.
public sealed record OrderPlaced(string? ThrowFrom = null) : INotification;

public sealed class FirstOrderPlacedHandler(CallLog log) : INotificationHandler<OrderPlaced>
{
    public async Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        log.Add($"{nameof(FirstOrderPlacedHandler)}:start");
        await Task.Yield();

        if (notification.ThrowFrom == nameof(FirstOrderPlacedHandler))
        {
            throw new TestException(nameof(FirstOrderPlacedHandler));
        }

        log.Add($"{nameof(FirstOrderPlacedHandler)}:end");
    }
}

public sealed class SecondOrderPlacedHandler(CallLog log) : INotificationHandler<OrderPlaced>
{
    public async Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        log.Add($"{nameof(SecondOrderPlacedHandler)}:start");
        await Task.Yield();

        if (notification.ThrowFrom == nameof(SecondOrderPlacedHandler))
        {
            throw new TestException(nameof(SecondOrderPlacedHandler));
        }

        log.Add($"{nameof(SecondOrderPlacedHandler)}:end");
    }
}

// Notification without handlers.
public sealed record NobodyListens : INotification;

// Notification used only by the cache tests.
public sealed record ParallelNotification(int Value) : INotification;

public sealed class ParallelNotificationHandler(CallLog log) : INotificationHandler<ParallelNotification>
{
    public async Task Handle(ParallelNotification notification, CancellationToken cancellationToken)
    {
        await Task.Yield();
        log.Add($"handler:ParallelNotification:{notification.Value}");
    }
}
