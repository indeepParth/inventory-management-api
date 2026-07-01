namespace InventoryManagement.Domain.Entities
{
    public class PurchaseItem
    {
        public int Id { get; set; }
        public int PurchaseId { get; set; }
        public Purchase Purchase { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotal { get; set; }
    }
}
