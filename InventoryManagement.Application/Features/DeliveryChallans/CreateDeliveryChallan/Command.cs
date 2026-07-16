using MediatR;

namespace InventoryManagement.Application.Features.DeliveryChallans.CreateDeliveryChallan
{
    public class Command : IRequest<DeliveryChallanResponse>
    {
        public string ChallanNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public DateTime ChallanDate { get; set; }
        public string? VehicleNumber { get; set; }
        public int? DriverId { get; set; }
        public string? DriverName { get; set; }
        public string DeliveryFromAddress { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public decimal DeliveryCharge { get; set; }
        public string? Notes { get; set; }
        public List<DeliveryChallanItemInput> Items { get; set; } = new();
    }

    public class DeliveryChallanItemInput
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
    }
}
