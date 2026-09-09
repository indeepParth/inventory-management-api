using AutoMapper;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.Products;
using InventoryManagement.Domain.Entities;
using MediatR;

namespace InventoryManagement.Application.Features.Products.CreateProduct
{
    public class Handler : IRequestHandler<Command, Response>
    {
        private readonly IProductRepository _repository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly ISubscriptionLimitService _subscriptionLimitService;

        public Handler(
            IProductRepository repository,
            ICategoryRepository categoryRepository,
            IUnitRepository unitRepository,
            ISubscriptionLimitService subscriptionLimitService)
        {
            _repository = repository;
            _categoryRepository = categoryRepository;
            _unitRepository = unitRepository;
            _subscriptionLimitService = subscriptionLimitService;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            await _subscriptionLimitService.EnsureCanCreateProductAsync(
                cancellationToken);

            var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);

            if (category is null)
            {
                throw new NotFoundException("Category not found.");
            }

            var unit = await _unitRepository.GetByIdAsync(request.BaseUnitId, cancellationToken);

            if (unit is null)
            {
                throw new NotFoundException("Unit not found.");
            }

            if (!unit.IsActive)
            {
                throw new BadRequestException("Unit is inactive.");
            }

            if (unit.BaseUnitId != unit.Id || unit.FactorToBaseUnit != 1m)
            {
                throw new BadRequestException("Product base unit must be a base unit.");
            }

            Product? baseProduct = null;
            if (request.BaseProductId.HasValue)
            {
                baseProduct = await _repository.GetProductByIdAsync(
                    request.BaseProductId.Value,
                    cancellationToken);
                if (baseProduct is null)
                {
                    throw new NotFoundException("Base product not found.");
                }

                if (baseProduct.BaseProductId.HasValue)
                {
                    throw new BadRequestException("Base product cannot be a sub-product.");
                }

                if (baseProduct.BaseUnitId != request.BaseUnitId)
                {
                    throw new BadRequestException("Sub-product must use the same base unit as its base product.");
                }
            }

            var product = new Product
            {
                Name = request.Name,
                SKU = request.SKU,
                Quantity = 0m,
                BaseUnitId = request.BaseUnitId,
                BaseProductId = baseProduct?.Id,
                BaseProduct = baseProduct,
                FactorToBaseProduct = baseProduct is null
                    ? null
                    : request.FactorToBaseProduct,
                DefaultSellingPrice = request.DefaultSellingPrice,
                AverageCost = 0m,
                CategoryId = request.CategoryId
            };

            await _repository.AddProductAsync(product);
            await _repository.SaveChangesAsync();

            return new Response
            {
                Id = product.Id,
                Name = product.Name,
                SKU = product.SKU,
                Quantity = product.Quantity,
                AvailableQuantity = ProductStock.GetAvailableQuantity(product),
                BaseUnitId = unit.Id,
                BaseUnitName = unit.Name,
                BaseProductId = product.BaseProductId,
                BaseProductName = product.BaseProduct?.Name,
                FactorToBaseProduct = product.FactorToBaseProduct,
                IsSubProduct = ProductStock.IsSubProduct(product),
                DefaultSellingPrice = product.DefaultSellingPrice,
                AverageCost = product.AverageCost,
                CategoryId = category.Id,
                CategoryName = category.Name
            };
        }
    }
}
