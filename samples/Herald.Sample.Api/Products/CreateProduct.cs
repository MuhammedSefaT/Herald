using Herald;

namespace Herald.Sample.Api.Products;

// Command that returns a value: the id of the created product.
public sealed record CreateProductCommand(string Name, decimal Price) : IRequest<Guid>;

public sealed class CreateProductHandler(ProductStore store, IPublisher publisher)
    : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product(Guid.NewGuid(), request.Name, request.Price);
        store.Add(product);

        // After the product is saved, every interested handler is notified.
        await publisher.Publish(new ProductCreatedNotification(product.Id, product.Name), cancellationToken);

        return product.Id;
    }
}
