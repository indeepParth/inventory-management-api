using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Products.GetProductById
{
    public class Handler : IRequestHandler<Query, Response>
    {
        private readonly IProductRepository _repository;

        public Handler(IProductRepository repository)
        {
            _repository = repository;
        }

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
        {
            var product = await _repository.GetProductByIdAsync(request.Id, cancellationToken);

            if (product is null)
            {
                throw new NotFoundException("Product not found");
            }

            return new Response
            {
                Id = product.Id,
                Name = product.Name,
                SKU = product.SKU,
                Quantity = product.Quantity,
                BaseUnit = product.BaseUnit,
                DefaultSellingPrice = product.DefaultSellingPrice,
                AverageCost = product.AverageCost,
                CategoryId = product.CategoryId,
                CategoryName = product.Category.Name,
                SupplierId = product.SupplierId,
                SupplierName = product.Supplier?.Name
            }; 
        }
    }
}
