using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Features.CustomerReturns
{
    internal static class CustomerReturnMapping
    {
        public static CustomerReturnResponse ToResponse(
            this CustomerReturn customerReturn)
        {
            return new CustomerReturnResponse
            {
                Id = customerReturn.Id,
                ReturnNumber = customerReturn.ReturnNumber,
                SalesInvoiceId = customerReturn.SalesInvoiceId,
                InvoiceNumber = customerReturn.SalesInvoice.InvoiceNumber,
                CustomerId = customerReturn.CustomerId,
                CustomerName = customerReturn.Customer.Name,
                ReturnDate = customerReturn.ReturnDate,
                Status = customerReturn.Status,
                Subtotal = customerReturn.Subtotal,
                TaxAmount = customerReturn.TaxAmount,
                GrandTotal = customerReturn.GrandTotal,
                Notes = customerReturn.Notes,
                CreatedAtUtc = customerReturn.CreatedAtUtc,
                UpdatedAtUtc = customerReturn.UpdatedAtUtc,
                PostedAtUtc = customerReturn.PostedAtUtc,
                CancelledAtUtc = customerReturn.CancelledAtUtc,
                CreatedBy = customerReturn.CreatedBy,
                Items = customerReturn.Items.Select(x =>
                    new CustomerReturnItemResponse
                    {
                        Id = x.Id,
                        SalesInvoiceItemId = x.SalesInvoiceItemId,
                        ProductId = x.ProductId,
                        ProductName = x.Product.Name,
                        ProductSku = x.Product.SKU,
                        Quantity = x.Quantity,
                        SellingUnitPrice = x.SellingUnitPrice,
                        TaxRate = x.TaxRate,
                        TaxAmount = x.TaxAmount,
                        LineTotal = x.LineTotal,
                        CostAtSale = x.CostAtSale
                    }).ToList()
            };
        }
    }
}
