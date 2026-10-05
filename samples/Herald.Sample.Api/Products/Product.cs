namespace Herald.Sample.Api.Products;

// Örnek uygulamanın tek varlığı.
public sealed record Product(Guid Id, string Name, decimal Price);
