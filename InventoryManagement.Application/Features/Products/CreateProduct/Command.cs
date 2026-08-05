using MediatR;

namespace InventoryManagement.Application.Features.Products.CreateProduct
{
    public class Command : IRequest<Response>
    {
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int BaseUnitId { get; set; }
        public int? BaseProductId { get; set; }
        public decimal? FactorToBaseProduct { get; set; }
        public decimal DefaultSellingPrice { get; set; }
        public int CategoryId { get; set; }
    }
}
