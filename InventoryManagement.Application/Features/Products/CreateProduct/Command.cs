using MediatR;

namespace InventoryManagement.Application.Features.Products.CreateProduct
{
    public class Command : IRequest<Responce>
    {
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}