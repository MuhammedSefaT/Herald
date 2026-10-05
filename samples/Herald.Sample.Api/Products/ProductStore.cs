using System.Collections.Concurrent;

namespace Herald.Sample.Api.Products;

// Veritabanı yerine kullanılan bellek içi depo. Singleton olarak kaydedilir.
public sealed class ProductStore
{
    private readonly ConcurrentDictionary<Guid, Product> _products = new();

    public void Add(Product product) => _products[product.Id] = product;

    public Product? Find(Guid id) => _products.GetValueOrDefault(id);

    public bool Remove(Guid id) => _products.TryRemove(id, out _);
}
