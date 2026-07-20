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
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = item.Product.Name,
                    ProductSku = item.Product.SKU,
                    Quantity = item.Quantity,
                    SellingUnitPrice = item.SellingUnitPrice,
                    TaxRate = item.TaxRate,
                    TaxAmount = item.TaxAmount,
                    LineTotal = item.LineTotal,
                    CostAtSale = item.CostAtSale,
                    DeliveryChallanItemId = item.DeliveryChallanItemId,
                    DeliveryChallanId = item.DeliveryChallanItem?.DeliveryChallanId,
                    DeliveryChallanNumber =
                        item.DeliveryChallanItem?.DeliveryChallan.ChallanNumber
                }).ToList()
            };
        }

        public static SalesInvoiceDetailResponse ToDetailResponse(
            this SalesInvoice invoice,
            IEnumerable<Payment> payments,
            IEnumerable<CustomerReturn> customerReturns)
        {
            var response = new SalesInvoiceDetailResponse
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
                Items = invoice.ToResponse().Items,
                SourceChallans = invoice.Items
                    .Where(item => item.DeliveryChallanItem?.DeliveryChallan != null)
                    .Select(item => item.DeliveryChallanItem!.DeliveryChallan)
                    .GroupBy(challan => challan.Id)
                    .Select(group => new SalesInvoiceSourceChallanResponse
                    {
                        Id = group.Key,
                        ChallanNumber = group.First().ChallanNumber
                    })
                    .OrderBy(challan => challan.ChallanNumber)
                    .ToList(),
                Payments = payments.Select(payment => new SalesInvoicePaymentResponse
                {
                    Id = payment.Id,
                    ReceiptNumber = payment.ReceiptNumber,
                    PaymentDate = payment.PaymentDate,
                    Amount = payment.Amount,
                    Method = payment.Method,
                    ExternalReference = payment.ExternalReference,
                    Note = payment.Note,
                    CreatedAtUtc = payment.CreatedAtUtc,
                    CreatedBy = payment.CreatedBy,
                    ReversesPaymentId = payment.ReversesPaymentId,
                    ReversalPaymentId = payment.Reversal?.Id
                }).ToList(),
                CustomerReturns = customerReturns.Select(customerReturn =>
                    new SalesInvoiceCustomerReturnResponse
                    {
                        Id = customerReturn.Id,
                        ReturnNumber = customerReturn.ReturnNumber,
                        ReturnDate = customerReturn.ReturnDate,
                        Status = customerReturn.Status,
                        Subtotal = customerReturn.Subtotal,
                        TaxAmount = customerReturn.TaxAmount,
                        GrandTotal = customerReturn.GrandTotal,
                        CreatedAtUtc = customerReturn.CreatedAtUtc,
                        UpdatedAtUtc = customerReturn.UpdatedAtUtc,
                        PostedAtUtc = customerReturn.PostedAtUtc,
                        CancelledAtUtc = customerReturn.CancelledAtUtc,
                        CreatedBy = customerReturn.CreatedBy
                    }).ToList()
            };

            return response;
        }
    }
}
