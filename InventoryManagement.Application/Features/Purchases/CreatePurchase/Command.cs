using MediatR;

namespace InventoryManagement.Application.Features.Purchases.CreatePurchase
{
    public class Command : IRequest<PurchaseResponse>
    {
        public string? PurchaseNumber { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierBillNumber { get; set; }
        public DateTime BillDate { get; set; }
        public decimal Discount { get; set; }
        public decimal OtherCharges { get; set; }
        public string? Notes { get; set; }
        public List<PurchaseItemInput> Items { get; set; } = new();
    }

    public class PurchaseItemInput
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TaxRate { get; set; }
    }
}
