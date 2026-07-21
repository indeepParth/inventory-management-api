using System.Globalization;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Services
{
    public class DocumentNumberService : IDocumentNumberService
    {
        private readonly ApplicationDbContext _context;

        public DocumentNumberService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateAsync(
            DocumentNumberType documentType,
            DateTime documentDate,
            bool isDirectInvoice = false,
            CancellationToken cancellationToken = default)
        {
            var year = documentDate.Year;
            var key = documentType.ToString();
            var prefix = GetPrefix(documentType);

            var sequence = await _context.DocumentSequences
                .SingleOrDefaultAsync(
                    x => x.DocumentType == key && x.Year == year,
                    cancellationToken);

            if (sequence is null)
            {
                sequence = new DocumentSequence
                {
                    DocumentType = key,
                    Year = year,
                    NextValue = await GetInitialNextValueAsync(
                        documentType,
                        prefix,
                        year,
                        cancellationToken)
                };

                await _context.DocumentSequences.AddAsync(sequence, cancellationToken);
            }

            var value = sequence.NextValue;
            sequence.NextValue++;

            await _context.SaveChangesAsync(cancellationToken);

            var number = $"{prefix}_{year}_{value:0000}";
            return documentType == DocumentNumberType.SalesInvoice && isDirectInvoice
                ? $"{number}_D"
                : number;
        }

        private async Task<int> GetInitialNextValueAsync(
            DocumentNumberType documentType,
            string prefix,
            int year,
            CancellationToken cancellationToken)
        {
            var numbers = documentType switch
            {
                DocumentNumberType.SalesInvoice => await _context.SalesInvoices
                    .AsNoTracking()
                    .Where(x => x.InvoiceNumber.StartsWith($"{prefix}_{year}_"))
                    .Select(x => x.InvoiceNumber)
                    .ToListAsync(cancellationToken),
                DocumentNumberType.DeliveryChallan => await _context.DeliveryChallans
                    .AsNoTracking()
                    .Where(x => x.ChallanNumber.StartsWith($"{prefix}_{year}_"))
                    .Select(x => x.ChallanNumber)
                    .ToListAsync(cancellationToken),
                DocumentNumberType.PaymentReceipt => await _context.Payments
                    .AsNoTracking()
                    .Where(x => x.ReceiptNumber.StartsWith($"{prefix}_{year}_"))
                    .Select(x => x.ReceiptNumber)
                    .ToListAsync(cancellationToken),
                DocumentNumberType.Purchase => await _context.Purchases
                    .AsNoTracking()
                    .Where(x => x.PurchaseNumber.StartsWith($"{prefix}_{year}_"))
                    .Select(x => x.PurchaseNumber)
                    .ToListAsync(cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(documentType))
            };

            var max = numbers
                .Select(number => TryParseSequenceValue(number, prefix, year))
                .DefaultIfEmpty(0)
                .Max();

            return max + 1;
        }

        private static int TryParseSequenceValue(string number, string prefix, int year)
        {
            var start = $"{prefix}_{year}_";
            if (!number.StartsWith(start, StringComparison.Ordinal))
            {
                return 0;
            }

            var suffix = number[start.Length..];
            if (suffix.EndsWith("_D", StringComparison.Ordinal))
            {
                suffix = suffix[..^2];
            }

            return int.TryParse(
                suffix,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var value)
                ? value
                : 0;
        }

        private static string GetPrefix(DocumentNumberType documentType) =>
            documentType switch
            {
                DocumentNumberType.SalesInvoice => "IN",
                DocumentNumberType.DeliveryChallan => "CH",
                DocumentNumberType.PaymentReceipt => "R",
                DocumentNumberType.Purchase => "P",
                _ => throw new ArgumentOutOfRangeException(nameof(documentType))
            };
    }
}
