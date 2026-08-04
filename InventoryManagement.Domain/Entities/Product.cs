namespace InventoryManagement.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public int BaseUnitId { get; set; }
        public Unit BaseUnit { get; set; } = null!;
        public decimal DefaultSellingPrice { get; set; }
        public decimal AverageCost { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
        public ICollection<ProductUnitConversion> UnitConversions { get; set; } =
            new List<ProductUnitConversion>();
    }
}
