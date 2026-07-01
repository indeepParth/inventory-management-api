using InventoryManagement.Application.Common.Models;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Purchases.GetPurchases
{
    public class Handler : IRequestHandler<Query, PagedResponse<PurchaseResponse>>
    {
        private readonly IPurchaseRepository _repository;

        public Handler(IPurchaseRepository repository)
        {
            _repository = repository;
        }

        public async Task<PagedResponse<PurchaseResponse>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var purchases = await _repository.GetAllAsync(
                request.PageNumber,
                request.PageSize,
                request.SupplierId,
                request.Status,
                request.DateFrom,
                request.DateTo,
                request.PurchaseNumber,
                request.SupplierBillNumber,
                cancellationToken);
            var totalCount = await _repository.GetCountAsync(
                request.SupplierId,
                request.Status,
                request.DateFrom,
                request.DateTo,
                request.PurchaseNumber,
                request.SupplierBillNumber,
                cancellationToken);

            return new PagedResponse<PurchaseResponse>
            {
                Items = purchases.Select(x => x.ToResponse()).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
