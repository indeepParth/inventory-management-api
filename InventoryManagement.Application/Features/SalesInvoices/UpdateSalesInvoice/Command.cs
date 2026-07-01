using MediatR;

namespace InventoryManagement.Application.Features.SalesInvoices.UpdateSalesInvoice
{
    public sealed record Command(
        int Id,
        string InvoiceNumber,
        int CustomerId,
        DateTime InvoiceDate,
        decimal Discount,
        decimal OtherCharges,
        string? Notes,
        List<SalesInvoiceItemInput> Items) : IRequest<SalesInvoiceResponse>;

    public class SalesInvoiceItemInput
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal SellingUnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public int? DeliveryChallanItemId { get; set; }
    }
}
