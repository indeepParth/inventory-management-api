using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Products.UpdateProduct
{
    public class Handler : IRequestHandler<Command, Responce>
    {
        private readonly IProductRepository _repository;

        public Handler(IProductRepository repository)
        {
            _repository = repository;
        }
        
        public async Task<Responce> Handle(Command request, CancellationToken cancellationToken)
        {
            var product = await _repository.GetProductByIdAsync(request.Id, cancellationToken);

            if (product is null)
            {
                throw new NotFoundException("Product not found");
            }

            product.Name = request.Name;
            product.SKU = request.SKU;
            product.Quantity = request.Quantity;
            product.Price = request.Price;

            await _repository.SaveChangesAsync(cancellationToken);

            return new Responce
            {
                Id = product.Id,
                Name = product.Name,
                SKU = product.SKU,
                Quantity = product.Quantity,
                Price = product.Price
            };
        }
    }
}