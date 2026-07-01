using MediatR;

namespace InventoryManagement.Application.Features.Purchases.CancelPurchase
{
    public class Command : IRequest<PurchaseResponse>
    {
        public int Id { get; set; }
    }
}
