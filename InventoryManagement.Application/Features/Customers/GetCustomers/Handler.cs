using InventoryManagement.Application.Common.Models;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Customers.GetCustomers
{
    public class Handler : IRequestHandler<Query, PagedResponse<CustomerResponse>>
    {
        private readonly ICustomerRepository _repository;

        public Handler(ICustomerRepository repository)
        {
            _repository = repository;
        }

        public async Task<PagedResponse<CustomerResponse>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var customers = await _repository.GetAllAsync(
                request.PageNumber,
                request.PageSize,
                request.Search,
                request.IsActive,
                cancellationToken);
            var totalCount = await _repository.GetCountAsync(
                request.Search,
                request.IsActive,
                cancellationToken);

            return new PagedResponse<CustomerResponse>
            {
                Items = customers.Select(x => x.ToResponse()).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
