using InventoryManagement.Application.Common.Models;
using MediatR;

namespace InventoryManagement.Application.Features.InventoryReports.GetCurrentStock;

public class Query : IRequest<PagedResponse<Response>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? CategoryId { get; set; }
    public string? Search { get; set; }
    public StockQuantityFilter? Stock { get; set; }
}
