using MediatR;

namespace InventoryManagement.Application.Features.SalesInvoices.PostSalesInvoice
{
    public class Command : IRequest<SalesInvoiceResponse>
    {
        public int Id { get; set; }
    }
}
