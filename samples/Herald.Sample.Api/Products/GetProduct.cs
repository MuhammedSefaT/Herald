using Herald;

namespace Herald.Sample.Api.Products;

// Query: ürün bulunamazsa null döner.
public sealed record GetProductQuery(Guid Id) : IRequest<Product?>;

public sealed class GetProductHandler(ProductStore store) : IRequestHandler<GetProductQuery, Product?>
{
    public Task<Product?> Handle(GetProductQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(store.Find(request.Id));
}
