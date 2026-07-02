using MediatR;

namespace InventoryManagement.Application.Features.StockMovements.RecordAdjustment
{
    public class Command : IRequest<ManualCorrectionResponse>
    {
        public int ProductId { get; set; }
        public decimal QuantityChange { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public string? Note { get; set; }
    }
}
