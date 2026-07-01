using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Entities
{
    public class DeliveryChallan
    {
        public int Id { get; set; }
        public string ChallanNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;
        public DateTime ChallanDate { get; set; }
        public DeliveryChallanStatus Status { get; set; }
        public string? VehicleNumber { get; set; }
        public string? DriverName { get; set; }
        public string DeliveryAddress { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public DateTime? PostedAtUtc { get; set; }
        public DateTime? CancelledAtUtc { get; set; }
        public DateTime? InvoicedAtUtc { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public ICollection<DeliveryChallanItem> Items { get; set; } =
            new List<DeliveryChallanItem>();
    }
}
