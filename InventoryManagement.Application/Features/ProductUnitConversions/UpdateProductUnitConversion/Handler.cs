using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.ProductUnitConversions.UpdateProductUnitConversion
{
    public class Handler : IRequestHandler<Command, Response>
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

        public async Task<Response> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetProductByIdAsync(
                request.ProductId,
                cancellationToken);

            if (product is null)
            {
                throw new NotFoundException("Product not found.");
            }

            var conversion = await _conversionRepository.GetByIdAsync(
                request.ProductId,
                request.Id,
                cancellationToken);

            if (conversion is null)
            {
                throw new NotFoundException("Product unit conversion not found.");
            }

            var isBaseUnit = conversion.UnitId == product.BaseUnitId;

            if (isBaseUnit && (!request.IsActive || request.FactorToBaseUnit != 1m))
            {
                throw new BadRequestException("Base unit conversion must stay active with factor 1.");
            }

            conversion.FactorToBaseUnit = isBaseUnit ? 1m : request.FactorToBaseUnit;
            conversion.IsActive = isBaseUnit || request.IsActive;

            await _conversionRepository.SaveChangesAsync(cancellationToken);

            return ProductUnitConversionMapping.ToResponse(
                conversion,
                product.BaseUnitId);
        }
    }
}
