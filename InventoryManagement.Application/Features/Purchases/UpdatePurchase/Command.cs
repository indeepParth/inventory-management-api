using MediatR;

namespace InventoryManagement.Application.Features.Purchases.UpdatePurchase
{
    public sealed record Command(
        int Id,
        string PurchaseNumber,
        int SupplierId,
        string? SupplierBillNumber,
        DateTime BillDate,
        decimal Discount,
        decimal OtherCharges,
        string? Notes,
        List<PurchaseItemInput> Items) : IRequest<PurchaseResponse>;

    public class PurchaseItemInput
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TaxRate { get; set; }
    }
}
