namespace InventoryManagement.Domain.Entities
{
    public class SalesInvoiceItem
    {
        public int Id { get; set; }
        public int SalesInvoiceId { get; set; }
        public SalesInvoice SalesInvoice { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public decimal Quantity { get; set; }
        public decimal SellingUnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotal { get; set; }
        public decimal? CostAtSale { get; set; }
        public int? DeliveryChallanItemId { get; set; }
        public DeliveryChallanItem? DeliveryChallanItem { get; set; }
        public bool IsChallanAllocationActive { get; set; }
    }
}
