using InventoryManagement.Application.Common.Models;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Drivers.GetDrivers
{
    public class Handler : IRequestHandler<Query, PagedResponse<DriverResponse>>
    {
        private readonly IDriverRepository _repository;

        public Handler(IDriverRepository repository)
        {
            _repository = repository;
        }

        public async Task<PagedResponse<DriverResponse>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var drivers = await _repository.GetAllAsync(
                request.PageNumber,
                request.PageSize,
                request.Search,
                request.IsActive,
                cancellationToken);
            var totalCount = await _repository.GetCountAsync(
                request.Search,
                request.IsActive,
                cancellationToken);

            return new PagedResponse<DriverResponse>
            {
                Items = drivers.Select(x => x.ToResponse()).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
