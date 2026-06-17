using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Products.GetProducts
{
    public class Handler : IRequestHandler<Query, List<Responce>>
    {
        private readonly IProductRepository _repository;

        public Handler(IProductRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Responce>> Handle(Query request, CancellationToken cancellationToken)
        {
            var products = await _repository.GetAllProductAsync(
                request.PageNumber,
                request.PageSize,
                request.Search,
                cancellationToken
            );

            return products
                .Select(p => new Responce
                {
                    Id = p.Id,
                    Name = p.Name,
                    SKU = p.SKU,
                    Quantity = p.Quantity,
                    Price = p.Price
                }).ToList();
        }
    }
}