using MediatR;

namespace InventoryManagement.Application.Features.DeliveryChallans.PostDeliveryChallan
{
    public class Command : IRequest<DeliveryChallanResponse>
    {
        public int Id { get; set; }
    }
}
