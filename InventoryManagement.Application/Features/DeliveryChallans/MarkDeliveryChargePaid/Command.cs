using MediatR;

namespace InventoryManagement.Application.Features.DeliveryChallans.MarkDeliveryChargePaid
{
    public class Command : IRequest<DeliveryChallanResponse>
    {
        public int Id { get; set; }
    }
}
