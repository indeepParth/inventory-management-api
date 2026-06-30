using MediatR;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Features.Products.CreateProduct
{
    public class Command : IRequest<Response>
    {
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public UnitOfMeasure BaseUnit { get; set; }
        public decimal DefaultSellingPrice { get; set; }
        public int CategoryId { get; set; }
        public int? SupplierId { get; set; }
    }
}
