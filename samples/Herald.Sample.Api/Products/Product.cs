namespace Herald.Sample.Api.Products;

// The only entity of the sample application.
public sealed record Product(Guid Id, string Name, decimal Price);
