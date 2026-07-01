using MediatR;

namespace InventoryManagement.Application.Features.Purchases.GetPurchaseById
{
    public class Query : IRequest<PurchaseResponse>
    {
        public int Id { get; set; }
    }
}
