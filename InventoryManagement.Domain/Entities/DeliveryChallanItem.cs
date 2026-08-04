namespace InventoryManagement.Domain.Entities
{
    public class DeliveryChallanItem
    {
        public int Id { get; set; }
        public int DeliveryChallanId { get; set; }
        public DeliveryChallan DeliveryChallan { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public decimal EnteredQuantity { get; set; }
        public int UnitId { get; set; }
        public Unit Unit { get; set; } = null!;
        public decimal ConvertedBaseQuantity { get; set; }
        public decimal Quantity
        {
            get => ConvertedBaseQuantity;
            set => ConvertedBaseQuantity = value;
        }
        public ICollection<SalesInvoiceItem> SalesInvoiceItems { get; set; } =
            new List<SalesInvoiceItem>();
    }
}
