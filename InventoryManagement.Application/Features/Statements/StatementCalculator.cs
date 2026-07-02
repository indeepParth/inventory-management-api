namespace InventoryManagement.Application.Features.Statements
{
    internal static class StatementCalculator
    {
        public static StatementResponse Calculate(
            int partyId,
            DateTime dateFrom,
            DateTime dateTo,
            int pageNumber,
            int pageSize,
            IEnumerable<StatementEntry> sourceEntries)
        {
            var fromInclusive = dateFrom.Date;
            var toExclusive = dateTo.Date.AddDays(1);
            var ordered = sourceEntries
                .OrderBy(x => x.TransactionDate)
                .ThenBy(x => x.TimestampUtc)
                .ThenBy(x => x.TransactionId)
                .ThenBy(x => x.Type)
                .ToList();

            var openingBalance = ordered
                .Where(x => x.TransactionDate < fromInclusive)
                .Sum(x => x.BalanceChange);
            var periodEntries = ordered
                .Where(x => x.TransactionDate >= fromInclusive &&
                            x.TransactionDate < toExclusive)
                .ToList();

            var runningBalance = openingBalance;
            foreach (var entry in periodEntries)
            {
                runningBalance += entry.BalanceChange;
                entry.RunningBalance = runningBalance;
            }

            return new StatementResponse
            {
                PartyId = partyId,
                DateFrom = fromInclusive,
                DateTo = dateTo.Date,
                OpeningBalance = openingBalance,
                ClosingBalance = runningBalance,
                TotalCharges = periodEntries
                    .Where(x => x.Type is StatementEntryType.Invoice or
                                StatementEntryType.Purchase)
                    .Sum(x => x.Amount),
                TotalPayments = periodEntries
                    .Where(x => x.Type == StatementEntryType.Payment)
                    .Sum(x => x.Amount),
                TotalReversals = periodEntries
                    .Where(x => x.Type == StatementEntryType.Reversal)
                    .Sum(x => Math.Abs(x.Amount)),
                Entries = periodEntries
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = periodEntries.Count
            };
        }
    }
}
