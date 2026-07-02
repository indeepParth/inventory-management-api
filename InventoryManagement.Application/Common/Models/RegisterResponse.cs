namespace InventoryManagement.Application.Common.Models;

public class RegisterResponse<T> : PagedResponse<T>
{
    public RegisterSummary Summary { get; set; } = new();
}

public class RegisterSummary
{
    public int DocumentCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal OtherCharges { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
}
