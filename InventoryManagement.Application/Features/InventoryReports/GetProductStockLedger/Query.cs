using InventoryManagement.Application.Common.Models;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.InventoryReports.GetProductStockLedger;

public class Query : IRequest<PagedResponse<Response>>
{
    public int ProductId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public StockMovementType? MovementType { get; set; }
}
