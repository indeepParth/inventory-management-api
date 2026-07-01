using MediatR;

namespace InventoryManagement.Application.Features.SalesInvoices.CancelSalesInvoice
{
    public class Command : IRequest<SalesInvoiceResponse>
    {
        public int Id { get; set; }
    }
}
