using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Models;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.Drivers.GetDriverDeliveries
{
    public class Handler : IRequestHandler<Query, DriverDeliveriesResponse>
    {
        private readonly IDriverRepository _drivers;
        private readonly IDeliveryChallanRepository _challans;
        private readonly ISalesInvoiceRepository _invoices;

        public Handler(
            IDriverRepository drivers,
            IDeliveryChallanRepository challans,
            ISalesInvoiceRepository invoices)
        {
            _drivers = drivers;
            _challans = challans;
            _invoices = invoices;
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
                1,
                int.MaxValue,
                cancellationToken);
            var invoices = await _invoices.GetDriverDeliveriesAsync(
                request.DriverId,
                request.DateFrom,
                request.DateTo,
                paidFilter,
                cancellationToken);
            var rows = challans.Select(x => new DriverDeliveryRowResponse
                {
                    SourceType = DriverDeliverySourceType.Challan,
                    DocumentId = x.Id,
                    DocumentNumber = x.ChallanNumber,
                    DocumentDate = x.ChallanDate,
                    ChallanId = x.Id,
                    ChallanNumber = x.ChallanNumber,
                    ChallanDate = x.ChallanDate,
                    Status = x.Status,
                    CustomerName = x.Customer.Name,
                    DeliveryFromAddress = x.DeliveryFromAddress,
                    DeliveryToAddress = x.DeliveryAddress,
                    VehicleNumber = x.VehicleNumber,
                    DeliveryCharge = x.DeliveryCharge,
                    LaborCharge = 0,
                    IsDeliveryChargePaid = x.IsDeliveryChargePaid,
                    ItemCount = x.Items.Count
                })
                .Concat(invoices.Select(x => new DriverDeliveryRowResponse
                {
                    SourceType = DriverDeliverySourceType.Invoice,
                    DocumentId = x.Id,
                    DocumentNumber = x.InvoiceNumber,
                    DocumentDate = x.InvoiceDate,
                    ChallanId = 0,
                    ChallanNumber = x.InvoiceNumber,
                    ChallanDate = x.InvoiceDate,
                    Status = DeliveryChallanStatus.Invoiced,
                    CustomerName = x.Customer.Name,
                    DeliveryFromAddress = string.Empty,
                    DeliveryToAddress = x.DeliveryAddress ?? string.Empty,
                    VehicleNumber = null,
                    DeliveryCharge = x.OtherCharges,
                    LaborCharge = x.LaborCharge,
                    IsDeliveryChargePaid = x.IsDeliveryChargePaid,
                    ItemCount = x.Items.Count
                }))
                .OrderByDescending(x => x.DocumentDate)
                .ThenByDescending(x => x.DocumentId)
                .ToList();
            var pageRows = rows
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new DriverDeliveriesResponse
            {
                Id = driver.Id,
                Name = driver.Name,
                Phone = driver.Phone,
                LicenseNumber = driver.LicenseNumber,
                IsActive = driver.IsActive,
                Deliveries = new PagedResponse<DriverDeliveryRowResponse>
                {
                    Items = pageRows,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalCount = rows.Count
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
