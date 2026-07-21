using MediatR;

namespace InventoryManagement.Application.Features.SalesInvoices.CreateFromChallans
{
    public class Command : IRequest<SalesInvoiceResponse>
    {
        public string? InvoiceNumber { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal Discount { get; set; }
        public decimal OtherCharges { get; set; }
        public string? Notes { get; set; }
        public List<ChallanItemInput> Items { get; set; } = new();
    }

    public class ChallanItemInput
    {
        public int DeliveryChallanItemId { get; set; }
        public decimal SellingUnitPrice { get; set; }
        public decimal TaxRate { get; set; }
    }
}
