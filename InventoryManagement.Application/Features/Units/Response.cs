namespace InventoryManagement.Application.Features.Units
{
    public class Response
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ShortName { get; set; }
        public decimal FactorToBaseUnit { get; set; }
        public int? BaseUnitId { get; set; }
        public string? BaseUnitName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
