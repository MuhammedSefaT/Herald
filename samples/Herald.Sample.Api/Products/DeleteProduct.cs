using Herald;

namespace Herald.Sample.Api.Products;

// Dönüş değeri olmayan command. Handler IRequestHandler<TRequest> uygular ve Unit görmez.
public sealed record DeleteProductCommand(Guid Id) : IRequest;

public sealed class DeleteProductHandler(ProductStore store, ILogger<DeleteProductHandler> logger)
    : IRequestHandler<DeleteProductCommand>
{
    public Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        if (store.Remove(request.Id))
        {
            logger.LogInformation("Ürün silindi: {ProductId}", request.Id);
        }

        return Task.CompletedTask;
    }
}
