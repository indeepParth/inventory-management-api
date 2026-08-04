using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using MediatR;

namespace InventoryManagement.Application.Features.ProductUnitConversions.CreateProductUnitConversion
{
    public class Handler : IRequestHandler<Command, Response>
    {
        private readonly IProductRepository _productRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IProductUnitConversionRepository _conversionRepository;

        public Handler(
            IProductRepository productRepository,
            IUnitRepository unitRepository,
            IProductUnitConversionRepository conversionRepository)
        {
            _productRepository = productRepository;
            _unitRepository = unitRepository;
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

            var unit = await _unitRepository.GetByIdAsync(
                request.UnitId,
                cancellationToken);

            if (unit is null)
            {
                throw new NotFoundException("Unit not found.");
            }

            if (!unit.IsActive)
            {
                throw new BadRequestException("Unit is inactive.");
            }

            var existing = await _conversionRepository.GetByProductAndUnitAsync(
                request.ProductId,
                request.UnitId,
                cancellationToken);

            if (existing is not null)
            {
                throw new BadRequestException("Product unit conversion already exists.");
            }

            if (request.UnitId == product.BaseUnitId && request.FactorToBaseUnit != 1m)
            {
                throw new BadRequestException("Base unit conversion factor must be 1.");
            }

            var conversion = new ProductUnitConversion
            {
                ProductId = request.ProductId,
                UnitId = request.UnitId,
                FactorToBaseUnit = request.UnitId == product.BaseUnitId
                    ? 1m
                    : request.FactorToBaseUnit,
                IsActive = true
            };

            await _conversionRepository.AddAsync(conversion, cancellationToken);
            await _conversionRepository.SaveChangesAsync(cancellationToken);

            conversion.Unit = unit;

            return ProductUnitConversionMapping.ToResponse(
                conversion,
                product.BaseUnitId);
        }
    }
}
