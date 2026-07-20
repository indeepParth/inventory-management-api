using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.SalesInvoices.GetSalesInvoiceById
{
    public class Handler : IRequestHandler<Query, SalesInvoiceDetailResponse>
    {
        private readonly ISalesInvoiceRepository _repository;
        private readonly IPaymentRepository _payments;
        private readonly ICustomerReturnRepository _customerReturns;

        public Handler(
            ISalesInvoiceRepository repository,
            IPaymentRepository payments,
            ICustomerReturnRepository customerReturns)
        {
            _repository = repository;
            _payments = payments;
            _customerReturns = customerReturns;
        }

        public async Task<SalesInvoiceDetailResponse> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var invoice = await _repository.GetByIdAsync(
                request.Id,
                cancellationToken);
            if (invoice == null)
            {
                throw new NotFoundException("Sales invoice not found.");
            }

            var payments = await _payments.GetBySalesInvoiceIdAsync(
                request.Id,
                cancellationToken);
            var customerReturns = await _customerReturns.GetBySalesInvoiceIdAsync(
                request.Id,
                cancellationToken);

            return invoice.ToDetailResponse(payments, customerReturns);
        }
    }
}
