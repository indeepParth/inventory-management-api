namespace InventoryManagement.Domain.Entities
{
    public class CustomerReturnItem
    {
        public int Id { get; set; }
        public int CustomerReturnId { get; set; }
        public CustomerReturn CustomerReturn { get; set; } = null!;
        public int SalesInvoiceItemId { get; set; }
        public SalesInvoiceItem SalesInvoiceItem { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public decimal Quantity { get; set; }
        public decimal SellingUnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotal { get; set; }
        public decimal CostAtSale { get; set; }
    }
}
