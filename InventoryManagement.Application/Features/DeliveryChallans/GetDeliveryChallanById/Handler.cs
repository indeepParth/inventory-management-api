using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.DeliveryChallans.GetDeliveryChallanById
{
    public class Handler : IRequestHandler<Query, DeliveryChallanResponse>
    {
        private readonly IDeliveryChallanRepository _repository;

        public Handler(IDeliveryChallanRepository repository) => _repository = repository;

        public async Task<DeliveryChallanResponse> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var challan = await _repository.GetByIdAsync(request.Id, cancellationToken);
            return challan?.ToResponse() ??
                throw new NotFoundException("Delivery challan not found.");
        }
    }
}
