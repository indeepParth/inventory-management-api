using InventoryManagement.Application.Common.Models;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.InventoryReports.GetPurchaseRegister;

public class Query : IRequest<RegisterResponse<Response>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? SupplierId { get; set; }
    public int? ProductId { get; set; }
    public PurchaseStatus? Status { get; set; }
    public bool IncludeDraftAndCancelledInSummary { get; set; }
}
