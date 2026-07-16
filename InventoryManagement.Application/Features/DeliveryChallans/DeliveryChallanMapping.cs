using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Features.DeliveryChallans
{
    internal static class DeliveryChallanMapping
    {
        public static DeliveryChallanResponse ToResponse(this DeliveryChallan challan) =>
            new()
            {
                Id = challan.Id,
                ChallanNumber = challan.ChallanNumber,
                CustomerId = challan.CustomerId,
                CustomerName = challan.Customer.Name,
                ChallanDate = challan.ChallanDate,
                Status = challan.Status,
                VehicleNumber = challan.VehicleNumber,
                DriverId = challan.DriverId,
                DriverName = challan.Driver?.Name ?? challan.DriverName,
                DeliveryFromAddress = challan.DeliveryFromAddress,
                DeliveryAddress = challan.DeliveryAddress,
                DeliveryCharge = challan.DeliveryCharge,
                IsDeliveryChargePaid = challan.IsDeliveryChargePaid,
                Notes = challan.Notes,
                CreatedAtUtc = challan.CreatedAtUtc,
                UpdatedAtUtc = challan.UpdatedAtUtc,
                PostedAtUtc = challan.PostedAtUtc,
                CancelledAtUtc = challan.CancelledAtUtc,
                InvoicedAtUtc = challan.InvoicedAtUtc,
                IsAvailableForInvoicing =
                    challan.Status == DeliveryChallanStatus.Posted &&
                    challan.Items.Count > 0 &&
                    challan.Items.All(item =>
                        item.SalesInvoiceItems.All(link =>
                            !link.IsChallanAllocationActive)),
                CreatedBy = challan.CreatedBy,
                Items = challan.Items.Select(x => new DeliveryChallanItemResponse
                {
                    Id = x.Id,
                    ProductId = x.ProductId,
                    ProductName = x.Product.Name,
                    ProductSku = x.Product.SKU,
                    Quantity = x.Quantity
                }).ToList()
            };
    }
}
