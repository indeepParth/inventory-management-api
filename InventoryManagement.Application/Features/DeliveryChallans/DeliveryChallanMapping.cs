using InventoryManagement.Domain.Entities;

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
                DriverName = challan.DriverName,
                DeliveryAddress = challan.DeliveryAddress,
                Notes = challan.Notes,
                CreatedAtUtc = challan.CreatedAtUtc,
                UpdatedAtUtc = challan.UpdatedAtUtc,
                PostedAtUtc = challan.PostedAtUtc,
                CancelledAtUtc = challan.CancelledAtUtc,
                InvoicedAtUtc = challan.InvoicedAtUtc,
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
