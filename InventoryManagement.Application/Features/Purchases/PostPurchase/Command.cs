using MediatR;

namespace InventoryManagement.Application.Features.Purchases.PostPurchase
{
    public class Command : IRequest<PurchaseResponse>
    {
        public int Id { get; set; }
    }
}
