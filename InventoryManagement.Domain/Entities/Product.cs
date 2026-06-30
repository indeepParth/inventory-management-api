
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public UnitOfMeasure BaseUnit { get; set; }
        public decimal DefaultSellingPrice { get; set; }
        public decimal AverageCost { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }
        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    }
}
