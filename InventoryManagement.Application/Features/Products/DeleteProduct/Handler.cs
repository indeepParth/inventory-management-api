using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Products.DeleteProduct
{
    public class Handler : IRequestHandler<Command, Response>
    {
        private readonly IProductRepository _repository;

        public Handler(IProductRepository repository)
        {
            _repository = repository;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            var product = await _repository.GetProductByIdAsync(request.Id, cancellationToken);

            if (product is null)
                throw new NotFoundException("Product is not found");

            await _repository.DeleteProductAsync(product, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            return new Response
            {
                Id = request.Id,
                Message = "Product Deleted Successfully."
            };
        }
    }
}