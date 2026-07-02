using InventoryManagement.Application.Common.Models;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Payments.GetPayments
{
    public class Handler : IRequestHandler<Query, PagedResponse<PaymentResponse>>
    {
        private readonly IPaymentRepository _payments;

        public Handler(IPaymentRepository payments)
        {
            _payments = payments;
        }

        public async Task<PagedResponse<PaymentResponse>> Handle(
            Query request, CancellationToken cancellationToken)
        {
            var items = await _payments.GetAllAsync(
                request.PageNumber, request.PageSize, request.CustomerId,
                request.SalesInvoiceId, request.Method, request.DateFrom,
                request.DateTo, request.ReceiptNumber, cancellationToken);
            var count = await _payments.GetCountAsync(
                request.CustomerId, request.SalesInvoiceId, request.Method,
                request.DateFrom, request.DateTo, request.ReceiptNumber,
                cancellationToken);
            return new PagedResponse<PaymentResponse>
            {
                Items = items.Select(x => x.ToResponse()).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = count
            };
        }
    }
}
