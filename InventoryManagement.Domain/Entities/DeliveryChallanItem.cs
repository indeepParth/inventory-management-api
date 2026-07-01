namespace InventoryManagement.Domain.Entities
{
    public class DeliveryChallanItem
    {
        public int Id { get; set; }
        public int DeliveryChallanId { get; set; }
        public DeliveryChallan DeliveryChallan { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public decimal Quantity { get; set; }
    }
}
