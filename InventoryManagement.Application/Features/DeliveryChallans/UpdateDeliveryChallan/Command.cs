using MediatR;

namespace InventoryManagement.Application.Features.DeliveryChallans.UpdateDeliveryChallan
{
    public sealed record Command(
        int Id,
        string ChallanNumber,
        int CustomerId,
        DateTime ChallanDate,
        string? VehicleNumber,
        string? DriverName,
        string DeliveryAddress,
        string? Notes,
        List<DeliveryChallanItemInput> Items) : IRequest<DeliveryChallanResponse>;

    public class DeliveryChallanItemInput
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
    }
}
