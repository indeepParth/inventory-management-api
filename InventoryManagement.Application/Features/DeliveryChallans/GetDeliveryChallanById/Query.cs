using MediatR;

namespace InventoryManagement.Application.Features.DeliveryChallans.GetDeliveryChallanById
{
    public class Query : IRequest<DeliveryChallanResponse>
    {
        public int Id { get; set; }
    }
}
