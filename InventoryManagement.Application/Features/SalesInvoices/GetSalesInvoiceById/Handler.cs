using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.SalesInvoices.GetSalesInvoiceById
{
    public class Handler : IRequestHandler<Query, SalesInvoiceResponse>
    {
        private readonly ISalesInvoiceRepository _repository;

        public Handler(ISalesInvoiceRepository repository)
        {
            _repository = repository;
        }

        public async Task<SalesInvoiceResponse> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var invoice = await _repository.GetByIdAsync(
                request.Id,
                cancellationToken);
            return invoice?.ToResponse() ??
                throw new NotFoundException("Sales invoice not found.");
        }
    }
}
