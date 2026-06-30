using InventoryManagement.Application.Common.Models;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Suppliers.GetSuppliers
{
    public class Handler : IRequestHandler<Query, PagedResponse<SupplierResponse>>
    {
        private readonly ISupplierRepository _repository;

        public Handler(ISupplierRepository repository)
        {
            _repository = repository;
        }

        public async Task<PagedResponse<SupplierResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var suppliers = await _repository.GetAllAsync(
                request.PageNumber, request.PageSize, request.Search, request.IsActive, cancellationToken);
            var totalCount = await _repository.GetCountAsync(request.Search, request.IsActive, cancellationToken);

            return new PagedResponse<SupplierResponse>
            {
                Items = suppliers.Select(x => x.ToResponse()).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
