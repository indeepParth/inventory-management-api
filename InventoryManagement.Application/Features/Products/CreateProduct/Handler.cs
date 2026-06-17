using AutoMapper;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using MediatR;

namespace InventoryManagement.Application.Features.Products.CreateProduct
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
            var product = new Product
            {
                Name = request.Name,
                SKU = request.SKU,
                Quantity = request.Quantity,
                Price = request.Price
            };

            await _repository.AddProductAsync(product);
            await _repository.SaveChangesAsync();

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