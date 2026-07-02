using MediatR;

namespace InventoryManagement.Application.Features.StockMovements.ReverseManualCorrection
{
    public class Command : IRequest<ManualCorrectionResponse>
    {
        public int Id { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public string? Note { get; set; }
    }
}
