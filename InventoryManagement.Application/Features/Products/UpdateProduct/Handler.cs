using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using MediatR;

namespace InventoryManagement.Application.Features.Products.UpdateProduct
{
    public class Handler : IRequestHandler<Command, Response>
    {
        private readonly IProductRepository _repository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ISupplierRepository _supplierRepository;

        public Handler(
            IProductRepository repository,
            ICategoryRepository categoryRepository,
            ISupplierRepository supplierRepository)
        {
            _repository = repository;
            _categoryRepository = categoryRepository;
            _supplierRepository = supplierRepository;
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

            Supplier? supplier = null;
            if (request.SupplierId.HasValue)
            {
                supplier = await _supplierRepository.GetByIdAsync(request.SupplierId.Value, cancellationToken);
                if (supplier is null)
                {
                    throw new NotFoundException("Supplier not found.");
                }

                if (!supplier.IsActive)
                {
                    throw new BadRequestException("Inactive supplier cannot be assigned to a product.");
                }
            }

            product.Name = request.Name;
            product.SKU = request.SKU;
            product.Quantity = request.Quantity;
            product.BaseUnit = request.BaseUnit;
            product.DefaultSellingPrice = request.DefaultSellingPrice;
            product.CategoryId = request.CategoryId;
            product.SupplierId = request.SupplierId;

            await _repository.SaveChangesAsync(cancellationToken);

            return new Response
            {
                Id = product.Id,
                Name = product.Name,
                SKU = product.SKU,
                Quantity = product.Quantity,
                BaseUnit = product.BaseUnit,
                DefaultSellingPrice = product.DefaultSellingPrice,
                AverageCost = product.AverageCost,
                CategoryId = category.Id,
                CategoryName = category.Name,
                SupplierId = supplier?.Id,
                SupplierName = supplier?.Name
            };
        }
    }
}
