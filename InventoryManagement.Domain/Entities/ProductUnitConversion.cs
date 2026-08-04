namespace InventoryManagement.Domain.Entities
{
    public class ProductUnitConversion
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int UnitId { get; set; }
        public Unit Unit { get; set; } = null!;
        public decimal FactorToBaseUnit { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
