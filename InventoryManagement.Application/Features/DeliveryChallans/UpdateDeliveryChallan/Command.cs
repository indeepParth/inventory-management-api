using MediatR;

namespace InventoryManagement.Application.Features.DeliveryChallans.UpdateDeliveryChallan
{
    public sealed record Command(
        int Id,
        string? ChallanNumber,
        int CustomerId,
        DateTime ChallanDate,
        string? VehicleNumber,
        int? DriverId,
        string? DriverName,
        string DeliveryFromAddress,
        string DeliveryAddress,
        decimal DeliveryCharge,
        string? Notes,
        List<DeliveryChallanItemInput> Items) : IRequest<DeliveryChallanResponse>;

    public class DeliveryChallanItemInput
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
    }
}
