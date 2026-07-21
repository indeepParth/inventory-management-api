namespace InventoryManagement.Application.Common.Interfaces
{
    public interface IDocumentNumberService
    {
        Task<string> GenerateAsync(
            DocumentNumberType documentType,
            DateTime documentDate,
            bool isDirectInvoice = false,
            CancellationToken cancellationToken = default);
    }
}
