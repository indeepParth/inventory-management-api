using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Models;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Drivers.GetDriverDeliveries
{
    public class Handler : IRequestHandler<Query, DriverDeliveriesResponse>
    {
        private readonly IDriverRepository _drivers;
        private readonly IDeliveryChallanRepository _challans;

        public Handler(
            IDriverRepository drivers,
            IDeliveryChallanRepository challans)
        {
            _drivers = drivers;
            _challans = challans;
        }

        public async Task<DriverDeliveriesResponse> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var driver = await _drivers.GetByIdAsync(request.DriverId, cancellationToken)
                ?? throw new NotFoundException("Driver not found.");
            var paidFilter = ToPaidFilter(request.PaymentStatus);
            var challans = await _challans.GetDriverDeliveriesAsync(
                request.DriverId,
                request.DateFrom,
                request.DateTo,
                paidFilter,
                request.PageNumber,
                request.PageSize,
                cancellationToken);
            var totalCount = await _challans.GetDriverDeliveriesCountAsync(
                request.DriverId,
                request.DateFrom,
                request.DateTo,
                paidFilter,
                cancellationToken);

            return new DriverDeliveriesResponse
            {
                Id = driver.Id,
                Name = driver.Name,
                Phone = driver.Phone,
                LicenseNumber = driver.LicenseNumber,
                IsActive = driver.IsActive,
                Deliveries = new PagedResponse<DriverDeliveryRowResponse>
                {
                    Items = challans.Select(x => new DriverDeliveryRowResponse
                    {
                        ChallanId = x.Id,
                        ChallanNumber = x.ChallanNumber,
                        ChallanDate = x.ChallanDate,
                        Status = x.Status,
                        CustomerName = x.Customer.Name,
                        DeliveryFromAddress = x.DeliveryFromAddress,
                        DeliveryToAddress = x.DeliveryAddress,
                        VehicleNumber = x.VehicleNumber,
                        DeliveryCharge = x.DeliveryCharge,
                        IsDeliveryChargePaid = x.IsDeliveryChargePaid,
                        ItemCount = x.Items.Count
                    }).ToList(),
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalCount = totalCount
                }
            };
        }

        private static bool? ToPaidFilter(DriverDeliveryPaymentStatus paymentStatus)
        {
            return paymentStatus switch
            {
                DriverDeliveryPaymentStatus.Paid => true,
                DriverDeliveryPaymentStatus.Unpaid => false,
                _ => null
            };
        }
    }
}
