namespace InventoryManagement.Domain.Entities
{
    public class SupplierReturnItem
    {
        public int Id { get; set; }
        public int SupplierReturnId { get; set; }
        public SupplierReturn SupplierReturn { get; set; } = null!;
        public int PurchaseItemId { get; set; }
        public PurchaseItem PurchaseItem { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotal { get; set; }
    }
}
