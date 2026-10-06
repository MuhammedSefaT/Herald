using Herald;

namespace Herald.Sample.Api.Products;

// Command that does not return a value. The handler implements IRequestHandler<TRequest> and never sees Unit.
public sealed record DeleteProductCommand(Guid Id) : IRequest;

public sealed class DeleteProductHandler(ProductStore store, ILogger<DeleteProductHandler> logger)
    : IRequestHandler<DeleteProductCommand>
{
    public Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        if (store.Remove(request.Id))
        {
            logger.LogInformation("Deleted product {ProductId}", request.Id);
        }

        return Task.CompletedTask;
    }
}
