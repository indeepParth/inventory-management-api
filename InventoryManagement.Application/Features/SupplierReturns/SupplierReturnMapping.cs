using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Features.SupplierReturns
{
    internal static class SupplierReturnMapping
    {
        public static SupplierReturnResponse ToResponse(
            this SupplierReturn supplierReturn)
        {
            return new SupplierReturnResponse
            {
                Id = supplierReturn.Id,
                ReturnNumber = supplierReturn.ReturnNumber,
                PurchaseId = supplierReturn.PurchaseId,
                PurchaseNumber = supplierReturn.Purchase.PurchaseNumber,
                SupplierId = supplierReturn.SupplierId,
                SupplierName = supplierReturn.Supplier.Name,
                ReturnDate = supplierReturn.ReturnDate,
                Status = supplierReturn.Status,
                Subtotal = supplierReturn.Subtotal,
                TaxAmount = supplierReturn.TaxAmount,
                GrandTotal = supplierReturn.GrandTotal,
                Notes = supplierReturn.Notes,
                CreatedAtUtc = supplierReturn.CreatedAtUtc,
                UpdatedAtUtc = supplierReturn.UpdatedAtUtc,
                PostedAtUtc = supplierReturn.PostedAtUtc,
                CancelledAtUtc = supplierReturn.CancelledAtUtc,
                CreatedBy = supplierReturn.CreatedBy,
                Items = supplierReturn.Items.Select(x =>
                    new SupplierReturnItemResponse
                    {
                        Id = x.Id,
                        PurchaseItemId = x.PurchaseItemId,
                        ProductId = x.ProductId,
                        ProductName = x.Product.Name,
                        ProductSku = x.Product.SKU,
                        Quantity = x.Quantity,
                        UnitCost = x.UnitCost,
                        TaxRate = x.TaxRate,
                        TaxAmount = x.TaxAmount,
                        LineTotal = x.LineTotal
                    }).ToList()
            };
        }
    }
}
