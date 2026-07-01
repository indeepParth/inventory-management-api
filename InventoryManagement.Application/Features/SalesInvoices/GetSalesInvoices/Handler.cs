using InventoryManagement.Application.Common.Models;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.SalesInvoices.GetSalesInvoices
{
    public class Handler : IRequestHandler<Query, PagedResponse<SalesInvoiceResponse>>
    {
        private readonly ISalesInvoiceRepository _repository;

        public Handler(ISalesInvoiceRepository repository)
        {
            _repository = repository;
        }

        public async Task<PagedResponse<SalesInvoiceResponse>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var invoices = await _repository.GetAllAsync(
                request.PageNumber,
                request.PageSize,
                request.CustomerId,
                request.Status,
                request.DateFrom,
                request.DateTo,
                request.InvoiceNumber,
                cancellationToken);
            var totalCount = await _repository.GetCountAsync(
                request.CustomerId,
                request.Status,
                request.DateFrom,
                request.DateTo,
                request.InvoiceNumber,
                cancellationToken);

            return new PagedResponse<SalesInvoiceResponse>
            {
                Items = invoices.Select(x => x.ToResponse()).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
