using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.Products;
using InventoryManagement.Domain.Entities;
using MediatR;

namespace InventoryManagement.Application.Features.Products.UpdateProduct
{
    public class Handler : IRequestHandler<Command, Response>
    {
        private readonly IProductRepository _repository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitRepository _unitRepository;

        public Handler(
            IProductRepository repository,
            ICategoryRepository categoryRepository,
            IUnitRepository unitRepository)
        {
            _repository = repository;
            _categoryRepository = categoryRepository;
            _unitRepository = unitRepository;
        }
        
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            var product = await _repository.GetProductByIdAsync(request.Id, cancellationToken);

            if (product is null)
            {
                throw new NotFoundException("Product not found");
            }

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
                if (request.BaseProductId.Value == product.Id)
                {
                    throw new BadRequestException("Product cannot be its own base product.");
                }

                if (product.SubProducts.Any())
                {
                    throw new BadRequestException("Product with sub-products cannot be converted to a sub-product.");
                }

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
            else if (product.SubProducts.Any() && request.BaseUnitId != product.BaseUnitId)
            {
                throw new BadRequestException("Product base unit cannot be changed while sub-products exist.");
            }

            product.Name = request.Name;
            product.SKU = request.SKU;
            product.BaseUnitId = request.BaseUnitId;
            product.BaseProductId = baseProduct?.Id;
            product.BaseProduct = baseProduct;
            product.FactorToBaseProduct = baseProduct is null
                ? null
                : request.FactorToBaseProduct;
            product.DefaultSellingPrice = request.DefaultSellingPrice;
            product.CategoryId = request.CategoryId;

            await _repository.SaveChangesAsync(cancellationToken);

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
