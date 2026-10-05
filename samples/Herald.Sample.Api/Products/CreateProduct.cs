using Herald;

namespace Herald.Sample.Api.Products;

// Dönüş değeri olan command: oluşturulan ürünün kimliğini döndürür.
public sealed record CreateProductCommand(string Name, decimal Price) : IRequest<Guid>;

public sealed class CreateProductHandler(ProductStore store, IPublisher publisher)
    : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product(Guid.NewGuid(), request.Name, request.Price);
        store.Add(product);

        // Ürün kaydedildikten sonra ilgilenen tüm handler'lara haber verilir.
        await publisher.Publish(new ProductCreatedNotification(product.Id, product.Name), cancellationToken);

        return product.Id;
    }
}
