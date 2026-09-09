namespace InventoryManagement.Domain.Entities
{
    public class Unit
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public string? ShortName { get; set; }
        public decimal FactorToBaseUnit { get; set; } = 1m;
        public int? BaseUnitId { get; set; }
        public Unit? BaseUnit { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; }
    }
}
