using MediatR;

namespace InventoryManagement.Application.Features.SalesInvoices.MarkDeliveryChargePaid
{
    public class Command : IRequest<SalesInvoiceResponse>
    {
        public int Id { get; set; }
    }
}
