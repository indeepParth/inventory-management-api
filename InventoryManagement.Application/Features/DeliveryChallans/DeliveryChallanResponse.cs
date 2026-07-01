using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Features.DeliveryChallans
{
    public class DeliveryChallanResponse
    {
        public int Id { get; set; }
        public string ChallanNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
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
        public List<DeliveryChallanItemResponse> Items { get; set; } = new();
    }

    public class DeliveryChallanItemResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
    }
}
