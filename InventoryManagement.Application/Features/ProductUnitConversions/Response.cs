namespace InventoryManagement.Application.Features.ProductUnitConversions
{
    public class Response
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public decimal FactorToBaseUnit { get; set; }
        public bool IsActive { get; set; }
        public bool IsBaseUnit { get; set; }
    }
}
