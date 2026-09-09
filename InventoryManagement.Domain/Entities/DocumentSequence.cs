namespace InventoryManagement.Domain.Entities
{
    public class DocumentSequence
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;
        public string DocumentType { get; set; } = string.Empty;
        public int Year { get; set; }
        public int NextValue { get; set; }
    }
}
