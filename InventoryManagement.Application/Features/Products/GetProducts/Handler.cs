using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagement.Application.Common.Models;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Products.GetProducts
{
    public class Handler : IRequestHandler<Query, PagedResponse<Response>>
    {
        private readonly IProductRepository _repository;

        public Handler(IProductRepository repository)
        {
            _repository = repository;
        }

        public async Task<PagedResponse<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var products = await _repository.GetAllProductAsync(
                request.PageNumber,
                request.PageSize,
                request.Search,
                cancellationToken
            );

            var totalCount = await _repository.GetProductCountAsync(
                request.Search,
                cancellationToken);

            var items = products
                .Select(p => new Response
                {
                    Id = p.Id,
                    Name = p.Name,
                    SKU = p.SKU,
                    Quantity = p.Quantity,
                    BaseUnit = p.BaseUnit,
                    DefaultSellingPrice = p.DefaultSellingPrice,
                    AverageCost = p.AverageCost,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name,
                    SupplierId = p.SupplierId,
                    SupplierName = p.Supplier?.Name
                }).ToList();

            return new PagedResponse<Response>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
