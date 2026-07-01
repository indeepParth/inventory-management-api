using InventoryManagement.Application.Common.Models;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.DeliveryChallans.GetDeliveryChallans
{
    public class Handler : IRequestHandler<Query, PagedResponse<DeliveryChallanResponse>>
    {
        private readonly IDeliveryChallanRepository _repository;

        public Handler(IDeliveryChallanRepository repository) => _repository = repository;

        public async Task<PagedResponse<DeliveryChallanResponse>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var items = await _repository.GetAllAsync(
                request.PageNumber, request.PageSize, request.CustomerId,
                request.Status, request.DateFrom, request.DateTo,
                request.ChallanNumber, cancellationToken);
            var count = await _repository.GetCountAsync(
                request.CustomerId, request.Status, request.DateFrom,
                request.DateTo, request.ChallanNumber, cancellationToken);

            return new PagedResponse<DeliveryChallanResponse>
            {
                Items = items.Select(x => x.ToResponse()).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = count
            };
        }
    }
}
