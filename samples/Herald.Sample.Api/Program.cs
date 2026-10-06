using Herald;
using Herald.Sample.Api.Behaviors;
using Herald.Sample.Api.Products;

var builder = WebApplication.CreateBuilder(args);

// Register Herald: every request and notification handler in this assembly is scanned,
// and the open generic logging behavior is added to the pipeline of every request.
builder.Services.AddHerald(configuration =>
{
    configuration.RegisterServicesFromAssemblyContaining<Program>();
    configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

builder.Services.AddSingleton<ProductStore>();

var app = builder.Build();

// Endpoints depend only on ISender; the business logic lives in the handlers.
app.MapPost("/products", async (CreateProductCommand command, ISender sender, CancellationToken cancellationToken) =>
{
    var id = await sender.Send(command, cancellationToken);
    return Results.Created($"/products/{id}", new { id });
});

app.MapGet("/products/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
{
    var product = await sender.Send(new GetProductQuery(id), cancellationToken);
    return product is null ? Results.NotFound() : Results.Ok(product);
});

app.MapDelete("/products/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
{
    await sender.Send(new DeleteProductCommand(id), cancellationToken);
    return Results.NoContent();
});

app.Run();
