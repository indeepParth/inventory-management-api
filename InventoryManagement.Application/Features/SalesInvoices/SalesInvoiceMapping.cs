using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Features.SalesInvoices
{
    internal static class SalesInvoiceMapping
    {
        public static SalesInvoiceResponse ToResponse(this SalesInvoice invoice)
        {
            return new SalesInvoiceResponse
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                CustomerId = invoice.CustomerId,
                CustomerName = invoice.Customer.Name,
                InvoiceDate = invoice.InvoiceDate,
                Status = invoice.Status,
                Subtotal = invoice.Subtotal,
                Discount = invoice.Discount,
                TaxAmount = invoice.TaxAmount,
                OtherCharges = invoice.OtherCharges,
                GrandTotal = invoice.GrandTotal,
                AmountPaid = invoice.AmountPaid,
                BalanceDue = invoice.BalanceDue,
                Notes = invoice.Notes,
                CreatedAtUtc = invoice.CreatedAtUtc,
                UpdatedAtUtc = invoice.UpdatedAtUtc,
                PostedAtUtc = invoice.PostedAtUtc,
                CancelledAtUtc = invoice.CancelledAtUtc,
                CreatedBy = invoice.CreatedBy,
                Items = invoice.Items.Select(item => new SalesInvoiceItemResponse
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product.Name,
                    ProductSku = item.Product.SKU,
                    Quantity = item.Quantity,
                    SellingUnitPrice = item.SellingUnitPrice,
                    TaxRate = item.TaxRate,
                    TaxAmount = item.TaxAmount,
                    LineTotal = item.LineTotal,
                    CostAtSale = item.CostAtSale,
                    DeliveryChallanItemId = item.DeliveryChallanItemId
                }).ToList()
            };
        }
    }
}
