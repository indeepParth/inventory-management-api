using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.DeliveryChallans.MarkDeliveryChargePaid
{
    public class Handler : IRequestHandler<Command, DeliveryChallanResponse>
    {
        private readonly IDeliveryChallanRepository _repository;

        public Handler(IDeliveryChallanRepository repository)
        {
            _repository = repository;
        }

        public async Task<DeliveryChallanResponse> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var challan = await _repository.GetForUpdateAsync(
                request.Id,
                cancellationToken) ?? throw new NotFoundException("Delivery challan not found.");

            if (challan.Status is not DeliveryChallanStatus.Posted and
                not DeliveryChallanStatus.Invoiced)
            {
                throw new BadRequestException(
                    "Only Posted or Invoiced delivery challans may have delivery charge marked paid.");
            }

            if (challan.DeliveryCharge <= 0)
            {
                throw new BadRequestException("Delivery charge must be greater than zero.");
            }

            if (challan.IsDeliveryChargePaid)
            {
                return challan.ToResponse();
            }

            challan.IsDeliveryChargePaid = true;
            challan.UpdatedAtUtc = DateTime.UtcNow;

            await _repository.SaveChangesAsync(cancellationToken);
            return challan.ToResponse();
        }
    }
}
