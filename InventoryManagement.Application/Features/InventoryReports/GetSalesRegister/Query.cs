using InventoryManagement.Application.Common.Models;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.InventoryReports.GetSalesRegister;

public class Query : IRequest<RegisterResponse<Response>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? CustomerId { get; set; }
    public int? ProductId { get; set; }
    public SalesSourceType? SourceType { get; set; }
    public SalesInvoiceStatus? Status { get; set; }
    public bool IncludeDraftAndCancelledInSummary { get; set; }
}
