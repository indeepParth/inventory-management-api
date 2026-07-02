namespace InventoryManagement.Application.Features.Statements
{
    public class StatementResponse
    {
        public int PartyId { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal TotalCharges { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal TotalReversals { get; set; }
        public IReadOnlyList<StatementEntry> Entries { get; set; } =
            Array.Empty<StatementEntry>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize <= 0
            ? 0
            : (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
