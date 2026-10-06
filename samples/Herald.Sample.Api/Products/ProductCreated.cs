using Herald;

namespace Herald.Sample.Api.Products;

// Notification published when a product is created. It can have more than one handler.
public sealed record ProductCreatedNotification(Guid ProductId, string Name) : INotification;

// First handler: logs the event.
public sealed class ProductCreatedLogHandler(ILogger<ProductCreatedLogHandler> logger)
    : INotificationHandler<ProductCreatedNotification>
{
    public Task Handle(ProductCreatedNotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Created product {ProductId} ({Name})", notification.ProductId, notification.Name);
        return Task.CompletedTask;
    }
}

// Second handler: a real application would send an email here; the sample only logs.
// Handlers run one after another in registration order.
public sealed class ProductCreatedEmailHandler(ILogger<ProductCreatedEmailHandler> logger)
    : INotificationHandler<ProductCreatedNotification>
{
    public Task Handle(ProductCreatedNotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sent new product email to the sales team: {Name}", notification.Name);
        return Task.CompletedTask;
    }
}
