using Herald;

namespace Herald.Sample.Api.Products;

// Bildirim: bir ürün oluşturulduğunda yayınlanır. Birden fazla handler'ı olabilir.
public sealed record ProductCreatedNotification(Guid ProductId, string Name) : INotification;

// Birinci handler: olayı loglar.
public sealed class ProductCreatedLogHandler(ILogger<ProductCreatedLogHandler> logger)
    : INotificationHandler<ProductCreatedNotification>
{
    public Task Handle(ProductCreatedNotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Ürün oluşturuldu: {ProductId} ({Name})", notification.ProductId, notification.Name);
        return Task.CompletedTask;
    }
}

// İkinci handler: gerçek bir uygulamada burada e-posta gönderilirdi; örnekte sadece loglanır.
// Handler'lar kayıt sırasıyla, birbiri ardına çalışır.
public sealed class ProductCreatedEmailHandler(ILogger<ProductCreatedEmailHandler> logger)
    : INotificationHandler<ProductCreatedNotification>
{
    public Task Handle(ProductCreatedNotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Satış ekibine yeni ürün e-postası gönderildi: {Name}", notification.Name);
        return Task.CompletedTask;
    }
}
