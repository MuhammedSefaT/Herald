# Herald

Herald, MediatR ile aynı kullanım şekline ve isimlendirmeye sahip, sıfırdan yazılmış bir .NET mediator kütüphanesidir. MediatR ticari lisansa geçtiği için kişisel projelerde onun yerine kullanılmak üzere yazıldı.

- İstek/handler (dönüş değerli ve dönüşsüz), bildirim (notification) ve pipeline behavior desteği
- `Microsoft.Extensions.DependencyInjection` ile `AddHerald` kaydı
- `net8.0` ve `net10.0`

| Paket | İçerik | Bağımlılık |
|---|---|---|
| `Herald.Abstractions` | `IRequest`, `IRequestHandler`, `INotification`, `INotificationHandler`, `IPipelineBehavior`, `ISender`, `IPublisher`, `IMediator`, `Unit` | Yok |
| `Herald` | `Mediator`, `AddHerald` | `Herald.Abstractions`, `Microsoft.Extensions.DependencyInjection.Abstractions` 8.x |

Tüm public tipler `Herald` namespace'indedir.

## Kurulum

Paketler NuGet.org'da yayınlanmaz, lokal bir klasörden kullanılır.

1. Herald reposunun kökünde paketleri üretin:

   ```bash
   dotnet pack -c Release -o ./artifacts
   ```

2. Herald'ı kullanacak solution'ın köküne bir `nuget.config` ekleyin. `herald` kaynağının yolunu kendi klonunuzun `artifacts` klasörüne göre değiştirin. Package source mapping sayesinde `Herald` ve `Herald.*` paketleri yalnızca lokal klasörden, diğer paketler nuget.org'dan gelir.

   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <packageSources>
       <clear />
       <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
       <add key="herald" value="/path/to/Herald\artifacts" />
     </packageSources>
     <packageSourceMapping>
       <packageSource key="nuget.org">
         <package pattern="*" />
       </packageSource>
       <packageSource key="herald">
         <package pattern="Herald" />
         <package pattern="Herald.*" />
       </packageSource>
     </packageSourceMapping>
   </configuration>
   ```

3. Paketleri ekleyin (hangi projeye hangisinin ekleneceği için [Katmanlar](#katmanlar) bölümüne bakın):

   ```bash
   dotnet add package Herald
   dotnet add package Herald.Abstractions
   ```

Aynı sürüm numarasıyla yeniden paketlerseniz NuGet önbellekteki eski paketi kullanmaya devam eder. Bu durumda `Directory.Build.props` içindeki `Version` değerini artırın veya `dotnet nuget locals global-packages --clear` çalıştırın.

## Kayıt

```csharp
using Herald;

builder.Services.AddHerald(configuration =>
{
    configuration.RegisterServicesFromAssemblyContaining<CreateOrderCommand>();
    configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
    configuration.Lifetime = ServiceLifetime.Scoped; // İsteğe bağlı, varsayılan Transient.
});
```

- Verilen assembly'lerdeki abstract ve açık generic olmayan tüm `IRequestHandler<,>`, `IRequestHandler<>` ve `INotificationHandler<>` sınıfları Transient olarak kaydedilir.
- Bir istek tipinin birden fazla handler'ı varsa `AddHerald` `InvalidOperationException` fırlatır.
- `IMediator` seçilen ömürle kaydedilir. `ISender` ve `IPublisher` aynı `IMediator` kaydına yönlendirilir.
- `AddHerald` birden fazla kez çağrılabilir. Daha önce eklenmiş kayıtlar tekrar eklenmez.

## Örnekler

### Dönüş değeri olan istek

```csharp
public sealed record CreateOrderCommand(string Product, int Quantity) : IRequest<Guid>;

public sealed class CreateOrderHandler(IOrderRepository orders) : IRequestHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = Order.Create(request.Product, request.Quantity);
        await orders.AddAsync(order, cancellationToken);
        return order.Id;
    }
}
```

### Dönüş değeri olmayan istek

```csharp
public sealed record CancelOrderCommand(Guid OrderId) : IRequest;

public sealed class CancelOrderHandler(IOrderRepository orders) : IRequestHandler<CancelOrderCommand>
{
    public Task Handle(CancelOrderCommand request, CancellationToken cancellationToken) =>
        orders.CancelAsync(request.OrderId, cancellationToken);
}
```

### İstek gönderme

```csharp
public sealed class OrderEndpoints(ISender sender)
{
    public async Task<Guid> Create(CancellationToken cancellationToken)
    {
        var orderId = await sender.Send(new CreateOrderCommand("Kalem", 3), cancellationToken);
        await sender.Send(new CancelOrderCommand(orderId), cancellationToken);
        return orderId;
    }
}
```

Tipi derleme zamanında bilinmeyen istekler için `Send(object)` kullanılır. Dönüşü olmayan isteklerde sonuç `null` olur.

### Bildirim

```csharp
public sealed record OrderCreated(Guid OrderId) : INotification;

public sealed class SendOrderEmail(IEmailSender email) : INotificationHandler<OrderCreated>
{
    public Task Handle(OrderCreated notification, CancellationToken cancellationToken) =>
        email.SendOrderCreatedAsync(notification.OrderId, cancellationToken);
}

await publisher.Publish(new OrderCreated(orderId), cancellationToken);
```

Handler'lar kayıt sırasıyla, birbiri ardına çalışır. Bir handler exception fırlatırsa exception çağırana iletilir ve sonraki handler'lar çalışmaz. Handler yoksa `Publish` hiçbir şey yapmaz.

### Pipeline behavior

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        logger.LogInformation("{Request} başladı", typeof(TRequest).Name);
        var response = await next();
        logger.LogInformation("{Request} bitti", typeof(TRequest).Name);
        return response;
    }
}
```

- Açık generic behavior `AddOpenBehavior(typeof(LoggingBehavior<,>))` ile, tek bir istek tipine özel behavior `AddBehavior<IPipelineBehavior<CreateOrderCommand, Guid>, CreateOrderValidation>()` ile eklenir.
- Behavior'lar eklenme sırasıyla çalışır. İlk eklenen en dışta, handler en içtedir.
- Dönüş değeri olmayan istekler behavior'larda `TResponse = Unit` olarak görünür. Handler'lar `Unit` görmez.
- `next()` parametresiz çağrılırsa behavior'a gelen `cancellationToken` iletilir. `next(token)` verilen token'ı iletir.

## Katmanlar

| Katman | Referans verdiği paket |
|---|---|
| Domain | Hiçbiri |
| Application | `Herald.Abstractions` |
| Infrastructure | `Herald.Abstractions` |
| API (composition root) | `Herald` |

İstekler, handler'lar, bildirimler ve behavior'lar yalnızca sözleşmelere ihtiyaç duyar. `AddHerald` çağrısı yalnızca composition root'ta yapılır. `Herald` paketi `Herald.Abstractions`'ı da getirir.

## MediatR'dan geçiş

1. Paket referanslarını değiştirin: `MediatR` yerine `Herald`, `MediatR.Contracts` yerine `Herald.Abstractions`.
2. Namespace'i ve kayıt metodunu değiştirin:

   ```diff
   - global using MediatR;
   + global using Herald;

   - services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
   + services.AddHerald(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
   ```

Herald, MediatR'ın şu özelliklerini içermez; bunları kullanan kod derlenmez ve yeniden yazılması gerekir:

- Stream istekleri (`IStreamRequest`, `CreateStream`)
- Pre/post processor'lar ve exception handler/action'lar
- Notification publisher stratejileri (ör. paralel yayın)

Ayrıca handler'lar bildirimin ve isteğin tam tipine göre çözümlenir: temel bir bildirim tipine yazılmış handler, türetilmiş bildirimler için çağrılmaz. Açık generic handler'lar taranmaz.
