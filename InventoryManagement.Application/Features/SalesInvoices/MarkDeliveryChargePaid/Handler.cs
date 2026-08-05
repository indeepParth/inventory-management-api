using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.SalesInvoices.MarkDeliveryChargePaid
{
    public class Handler : IRequestHandler<Command, SalesInvoiceResponse>
    {
        private readonly ISalesInvoiceRepository _repository;

        public Handler(ISalesInvoiceRepository repository)
        {
            _repository = repository;
        }

        public async Task<SalesInvoiceResponse> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var invoice = await _repository.GetForDeliveryChargeUpdateAsync(
                request.Id,
                cancellationToken) ?? throw new NotFoundException("Sales invoice not found.");

            if (invoice.Status is not SalesInvoiceStatus.Posted and
                not SalesInvoiceStatus.PartiallyPaid and
                not SalesInvoiceStatus.Paid)
            {
                throw new BadRequestException(
                    "Only Posted, Partially paid, or Paid sales invoices may have driver charge marked paid.");
            }

            if (!invoice.DriverId.HasValue)
            {
                throw new BadRequestException("Sales invoice does not have a driver.");
            }

            if (invoice.OtherCharges <= 0)
            {
                throw new BadRequestException("Driver charge must be greater than zero.");
            }

            if (invoice.IsDeliveryChargePaid)
            {
                return invoice.ToResponse();
            }

            invoice.IsDeliveryChargePaid = true;
            invoice.UpdatedAtUtc = DateTime.UtcNow;

            await _repository.SaveChangesAsync(cancellationToken);
            return invoice.ToResponse();
        }
    }
}
