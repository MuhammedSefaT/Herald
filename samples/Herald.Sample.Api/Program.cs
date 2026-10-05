using Herald;
using Herald.Sample.Api.Behaviors;
using Herald.Sample.Api.Products;

var builder = WebApplication.CreateBuilder(args);

// Herald kaydı: bu assembly'deki tüm request ve notification handler'ları taranır,
// açık generic logging behavior'ı tüm isteklerin pipeline'ına eklenir.
builder.Services.AddHerald(configuration =>
{
    configuration.RegisterServicesFromAssemblyContaining<Program>();
    configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

builder.Services.AddSingleton<ProductStore>();

var app = builder.Build();

// Endpoint'ler sadece ISender'a bağımlıdır; iş mantığı handler'lardadır.
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
