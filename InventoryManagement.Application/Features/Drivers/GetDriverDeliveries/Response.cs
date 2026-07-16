using InventoryManagement.Application.Common.Models;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Features.Drivers.GetDriverDeliveries
{
    public class DriverDeliveriesResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? LicenseNumber { get; set; }
        public bool IsActive { get; set; }
        public PagedResponse<DriverDeliveryRowResponse> Deliveries { get; set; } = new();
    }

    public class DriverDeliveryRowResponse
    {
        public int ChallanId { get; set; }
        public string ChallanNumber { get; set; } = string.Empty;
        public DateTime ChallanDate { get; set; }
        public DeliveryChallanStatus Status { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string DeliveryFromAddress { get; set; } = string.Empty;
        public string DeliveryToAddress { get; set; } = string.Empty;
        public string? VehicleNumber { get; set; }
        public decimal DeliveryCharge { get; set; }
        public bool IsDeliveryChargePaid { get; set; }
        public int ItemCount { get; set; }
    }
}
