using InventoryManagement.Application.Common.Models;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.Payments.GetPayments
{
    public class Query : IRequest<PagedResponse<PaymentResponse>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? CustomerId { get; set; }
        public int? SalesInvoiceId { get; set; }
        public PaymentMethod? Method { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? ReceiptNumber { get; set; }
    }
}
