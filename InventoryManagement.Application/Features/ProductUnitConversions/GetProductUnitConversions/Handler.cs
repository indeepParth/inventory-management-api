using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.ProductUnitConversions.GetProductUnitConversions
{
    public class Handler : IRequestHandler<Query, List<Response>>
    {
        private readonly IProductRepository _productRepository;
        private readonly IProductUnitConversionRepository _conversionRepository;

        public Handler(
            IProductRepository productRepository,
            IProductUnitConversionRepository conversionRepository)
        {
            _productRepository = productRepository;
            _conversionRepository = conversionRepository;
        }

        public async Task<List<Response>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetProductByIdAsync(
                request.ProductId,
                cancellationToken);

            if (product is null)
            {
                throw new NotFoundException("Product not found.");
            }

            var conversions = await _conversionRepository.GetByProductIdAsync(
                request.ProductId,
                cancellationToken);

            return conversions
                .Select(x => ProductUnitConversionMapping.ToResponse(
                    x,
                    product.BaseUnitId))
                .ToList();
        }
    }
}
