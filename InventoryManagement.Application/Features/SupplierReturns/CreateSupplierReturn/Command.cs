using MediatR;

namespace InventoryManagement.Application.Features.SupplierReturns.CreateSupplierReturn
{
    public class Command : IRequest<SupplierReturnResponse>
    {
        public string ReturnNumber { get; set; } = string.Empty;
        public int PurchaseId { get; set; }
        public DateTime ReturnDate { get; set; }
        public string? Notes { get; set; }
        public List<SupplierReturnItemInput> Items { get; set; } = new();
    }

    public class SupplierReturnItemInput
    {
        public int PurchaseItemId { get; set; }
        public decimal Quantity { get; set; }
    }
}
