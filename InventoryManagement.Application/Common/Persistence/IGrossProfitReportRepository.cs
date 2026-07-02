namespace InventoryManagement.Application.Common.Persistence;

public interface IGrossProfitReportRepository
{
    Task<IReadOnlyList<GrossProfitTransaction>> GetTransactionsAsync(
        DateTime? fromDate,
        DateTime? toDate,
        int? invoiceId,
        int? productId,
        int? categoryId,
        int? customerId,
        CancellationToken cancellationToken = default);
}

public class GrossProfitTransaction
{
    public DateTime Date { get; set; }
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal SellingUnitPrice { get; set; }
    public decimal CostAtSale { get; set; }
    public bool IsReturn { get; set; }
}
