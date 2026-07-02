namespace InventoryManagement.Application.Features.Statements
{
    public class StatementEntry
    {
        public StatementEntryType Type { get; set; }
        public int TransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string? ExternalReference { get; set; }
        public string? Note { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceChange { get; set; }
        public decimal RunningBalance { get; set; }
    }
}
