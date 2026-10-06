# Herald

> **Note:** Herald is developed for my personal projects. It is MIT licensed and provided as is: no support is offered, and backward compatibility between versions is not guaranteed.

Herald is a lightweight mediator library for .NET. It sends requests to their handlers, publishes notifications to every interested handler, and runs cross-cutting concerns such as logging and validation through pipeline behaviors.

- Requests with and without a response, notifications and pipeline behaviors
- Registration with `AddHerald` on `Microsoft.Extensions.DependencyInjection`
- Targets `net8.0` and `net10.0`

| Package | Contents | Dependencies |
|---|---|---|
| `Herald.Abstractions` | `IRequest`, `IRequestHandler`, `INotification`, `INotificationHandler`, `IPipelineBehavior`, `ISender`, `IPublisher`, `IHerald`, `Unit` | None |
| `Herald` | `AddHerald`, `HeraldConfiguration` | `Herald.Abstractions`, `Microsoft.Extensions.DependencyInjection.Abstractions` 8.x |

All public types are in the `Herald` namespace.

## Installation

### From nuget.org

Herald requires .NET 8 or later. Add the package that each project needs (see [Layers](#layers)):

```bash
# The project that registers services (for example, the API or host project)
dotnet add package Herald

# Projects that only define or use requests, handlers, notifications and behaviors
dotnet add package Herald.Abstractions
```

Or add the references to the project file:

```xml
<ItemGroup>
  <PackageReference Include="Herald" Version="1.0.0" />
</ItemGroup>
```

The `Herald` package brings in `Herald.Abstractions`, so a project that references `Herald` does not need both.

### From source

To use a local build instead of nuget.org:

1. Build the packages in the root of the Herald repository:

   ```bash
   dotnet pack -c Release -o ./artifacts
   ```

2. Add a `nuget.config` file to the root of the solution that uses Herald. Replace the example path `/path/to/Herald/artifacts` with the `artifacts` folder of your clone. The path can be absolute or relative to `nuget.config`. With package source mapping, `Herald` and `Herald.*` come only from the local folder and every other package comes from nuget.org.

   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <packageSources>
       <clear />
       <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
       <add key="herald" value="/path/to/Herald/artifacts" />
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

3. Add the packages with `dotnet add package` as shown above.

If you pack again with the same version number, NuGet keeps using the cached package. Increase `Version` in `Directory.Build.props` or run `dotnet nuget locals global-packages --clear`.

## Registration

```csharp
using Herald;

builder.Services.AddHerald(configuration =>
{
    configuration.RegisterServicesFromAssemblyContaining<CreateOrderCommand>();
    configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
    configuration.Lifetime = ServiceLifetime.Scoped; // Optional. The default is Transient.
});
```

- Every non-abstract, non-open-generic class in the given assemblies that implements `IRequestHandler<,>`, `IRequestHandler<>` or `INotificationHandler<>` is registered as transient.
- If a request type has more than one handler, `AddHerald` throws `InvalidOperationException`.
- `IHerald` is registered with the configured lifetime. `ISender` and `IPublisher` resolve the same `IHerald` registration.
- `AddHerald` can be called more than once. Registrations that already exist are not added again.

## Usage

### Request with a response

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

### Request without a response

```csharp
public sealed record CancelOrderCommand(Guid OrderId) : IRequest;

public sealed class CancelOrderHandler(IOrderRepository orders) : IRequestHandler<CancelOrderCommand>
{
    public Task Handle(CancelOrderCommand request, CancellationToken cancellationToken) =>
        orders.CancelAsync(request.OrderId, cancellationToken);
}
```

### Sending requests

```csharp
public sealed class OrderEndpoints(ISender sender)
{
    public async Task<Guid> Create(CancellationToken cancellationToken)
    {
        var orderId = await sender.Send(new CreateOrderCommand("Pencil", 3), cancellationToken);
        await sender.Send(new CancelOrderCommand(orderId), cancellationToken);
        return orderId;
    }
}
```

Use `Send(object)` for requests whose type is not known at compile time. For a request without a response, the result is `null`.

### Notifications

```csharp
public sealed record OrderCreated(Guid OrderId) : INotification;

public sealed class SendOrderEmail(IEmailSender email) : INotificationHandler<OrderCreated>
{
    public Task Handle(OrderCreated notification, CancellationToken cancellationToken) =>
        email.SendOrderCreatedAsync(notification.OrderId, cancellationToken);
}

await publisher.Publish(new OrderCreated(orderId), cancellationToken);
```

Handlers run one after another in registration order. If a handler throws, the exception reaches the caller and the remaining handlers do not run. If there is no handler, `Publish` does nothing.

### Pipeline behaviors

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
        var response = await next();
        logger.LogInformation("Handled {Request}", typeof(TRequest).Name);
        return response;
    }
}
```

- Add an open generic behavior for all requests with `AddOpenBehavior(typeof(LoggingBehavior<,>))`. Add a behavior for one request type with `AddBehavior<IPipelineBehavior<CreateOrderCommand, Guid>, CreateOrderValidation>()`.
- Behaviors run in the order they are added. The first one added is outermost; the handler is innermost.
- Requests without a response appear in behaviors with `TResponse = Unit`. Handlers never see `Unit`.
- When `next()` is called without arguments, the `cancellationToken` the behavior received is passed on. `next(token)` passes on the given token.

> **Warning:** Do not constrain `TRequest` to `IRequest<TResponse>` in pipeline behaviors; the behavior is silently skipped for requests without a response. Use `where TRequest : notnull` instead.

## Layers

| Layer | Package reference |
|---|---|
| Domain | None |
| Application | `Herald.Abstractions` |
| Infrastructure | `Herald.Abstractions` |
| API (composition root) | `Herald` |

Requests, handlers, notifications and behaviors only need the contracts. Call `AddHerald` only in the composition root.

## Moving an existing project to Herald

In a project that already uses the `IRequest`, `IRequestHandler`, `INotification`, `INotificationHandler`, `IPipelineBehavior`, `ISender`, `IPublisher` and `Unit` contracts:

1. Remove the references to the previous mediator packages and add `Herald` and `Herald.Abstractions`.
2. Change the namespace in the `global using` line to `Herald` and register the services with `AddHerald`:

   ```csharp
   global using Herald;

   services.AddHerald(configuration => configuration.RegisterServicesFromAssemblyContaining<Program>());
   ```

3. Use `IHerald` where you need one interface that both sends requests and publishes notifications. Code that uses `ISender` and `IPublisher` does not change.

## Not included

Herald does not provide:

- Stream requests
- Pre/post processors and exception handlers
- Custom publishing strategies, such as publishing notifications in parallel

Handlers are resolved by the exact type of the request or notification: a handler written for a base notification type is not called for derived notifications. Open generic handlers are not scanned.

## License

Herald is licensed under the [MIT License](https://github.com/MuhammedSefaT/Herald/blob/main/LICENSE).
