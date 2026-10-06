using System.Collections.Concurrent;

namespace Herald.Sample.Api.Products;

// In-memory store used instead of a database. Registered as a singleton.
public sealed class ProductStore
{
    private readonly ConcurrentDictionary<Guid, Product> _products = new();

    public void Add(Product product) => _products[product.Id] = product;

    public Product? Find(Guid id) => _products.GetValueOrDefault(id);

    public bool Remove(Guid id) => _products.TryRemove(id, out _);
}
