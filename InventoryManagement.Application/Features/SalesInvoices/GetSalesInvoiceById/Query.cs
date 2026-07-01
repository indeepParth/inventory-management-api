using MediatR;

namespace InventoryManagement.Application.Features.SalesInvoices.GetSalesInvoiceById
{
    public class Query : IRequest<SalesInvoiceResponse>
    {
        public int Id { get; set; }
    }
}
