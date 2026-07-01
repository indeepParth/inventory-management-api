using InventoryManagement.Application.Common.Models;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.Purchases.GetPurchases
{
    public class Query : IRequest<PagedResponse<PurchaseResponse>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? SupplierId { get; set; }
        public PurchaseStatus? Status { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? PurchaseNumber { get; set; }
        public string? SupplierBillNumber { get; set; }
    }
}
