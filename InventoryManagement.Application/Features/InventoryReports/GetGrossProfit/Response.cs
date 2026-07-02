namespace InventoryManagement.Application.Features.InventoryReports.GetGrossProfit;

public class Response
{
    public string ReportName { get; set; } = "Gross Profit";
    public GrossProfitValues Summary { get; set; } = new();
    public IReadOnlyList<InvoiceBreakdown> ByInvoice { get; set; } =
        Array.Empty<InvoiceBreakdown>();
    public IReadOnlyList<ProductBreakdown> ByProduct { get; set; } =
        Array.Empty<ProductBreakdown>();
    public IReadOnlyList<CategoryBreakdown> ByCategory { get; set; } =
        Array.Empty<CategoryBreakdown>();
    public IReadOnlyList<CustomerBreakdown> ByCustomer { get; set; } =
        Array.Empty<CustomerBreakdown>();
}

public class GrossProfitValues
{
    public decimal SoldQuantity { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal NetQuantity { get; set; }
    public decimal Revenue { get; set; }
    public decimal Returns { get; set; }
    public decimal NetRevenue { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal ReturnedCost { get; set; }
    public decimal NetCostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossMarginPercentage { get; set; }
}

public class InvoiceBreakdown : GrossProfitValues
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
}

public class ProductBreakdown : GrossProfitValues
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
}

public class CategoryBreakdown : GrossProfitValues
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

public class CustomerBreakdown : GrossProfitValues
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
}
