namespace InventoryManagement.Application.Features.Products.GetProductById
{
    public class Response
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AvailableQuantity { get; set; }
        public int BaseUnitId { get; set; }
        public string BaseUnitName { get; set; } = string.Empty;
        public int? BaseProductId { get; set; }
        public string? BaseProductName { get; set; }
        public decimal? FactorToBaseProduct { get; set; }
        public bool IsSubProduct { get; set; }
        public decimal DefaultSellingPrice { get; set; }
        public decimal AverageCost { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}
