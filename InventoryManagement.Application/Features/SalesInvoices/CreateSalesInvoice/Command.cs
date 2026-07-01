using MediatR;

namespace InventoryManagement.Application.Features.SalesInvoices.CreateSalesInvoice
{
    public class Command : IRequest<SalesInvoiceResponse>
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal Discount { get; set; }
        public decimal OtherCharges { get; set; }
        public string? Notes { get; set; }
        public List<SalesInvoiceItemInput> Items { get; set; } = new();
    }

    public class SalesInvoiceItemInput
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal SellingUnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public int? DeliveryChallanItemId { get; set; }
    }
}
