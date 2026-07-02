using MediatR;

namespace InventoryManagement.Application.Features.InventoryReports.GetGrossProfit;

public class Query : IRequest<Response>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? InvoiceId { get; set; }
    public int? ProductId { get; set; }
    public int? CategoryId { get; set; }
    public int? CustomerId { get; set; }
}
