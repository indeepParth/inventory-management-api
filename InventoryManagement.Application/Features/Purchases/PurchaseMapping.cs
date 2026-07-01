using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Features.Purchases
{
    internal static class PurchaseMapping
    {
        public static PurchaseResponse ToResponse(this Purchase purchase)
        {
            return new PurchaseResponse
            {
                Id = purchase.Id,
                PurchaseNumber = purchase.PurchaseNumber,
                SupplierId = purchase.SupplierId,
                SupplierName = purchase.Supplier.Name,
                SupplierBillNumber = purchase.SupplierBillNumber,
                BillDate = purchase.BillDate,
                Status = purchase.Status,
                Subtotal = purchase.Subtotal,
                Discount = purchase.Discount,
                TaxAmount = purchase.TaxAmount,
                OtherCharges = purchase.OtherCharges,
                GrandTotal = purchase.GrandTotal,
                Notes = purchase.Notes,
                CreatedAtUtc = purchase.CreatedAtUtc,
                PostedAtUtc = purchase.PostedAtUtc,
                CancelledAtUtc = purchase.CancelledAtUtc,
                CreatedBy = purchase.CreatedBy,
                Items = purchase.Items.Select(item => new PurchaseItemResponse
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product.Name,
                    ProductSku = item.Product.SKU,
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost,
                    TaxRate = item.TaxRate,
                    TaxAmount = item.TaxAmount,
                    LineTotal = item.LineTotal
                }).ToList()
            };
        }
    }
}
