using MediatR;

namespace InventoryManagement.Application.Features.SalesInvoices.GetSalesInvoiceById
{
    public class Query : IRequest<SalesInvoiceDetailResponse>
    {
        public int Id { get; set; }
    }
}
