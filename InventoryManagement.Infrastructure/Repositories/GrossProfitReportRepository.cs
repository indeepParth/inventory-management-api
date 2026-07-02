using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories;

public class GrossProfitReportRepository : IGrossProfitReportRepository
{
    private readonly ApplicationDbContext _context;

    public GrossProfitReportRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<GrossProfitTransaction>> GetTransactionsAsync(
        DateTime? fromDate,
        DateTime? toDate,
        int? invoiceId,
        int? productId,
        int? categoryId,
        int? customerId,
        CancellationToken cancellationToken = default)
    {
        var sales = _context.SalesInvoiceItems
            .AsNoTracking()
            .Where(x =>
                x.SalesInvoice.Status != SalesInvoiceStatus.Draft &&
                x.SalesInvoice.Status != SalesInvoiceStatus.Cancelled &&
                x.CostAtSale.HasValue);

        if (fromDate.HasValue)
            sales = sales.Where(x =>
                x.SalesInvoice.InvoiceDate >= fromDate.Value);
        if (toDate.HasValue)
            sales = sales.Where(x =>
                x.SalesInvoice.InvoiceDate <= toDate.Value);
        if (invoiceId.HasValue)
            sales = sales.Where(x => x.SalesInvoiceId == invoiceId.Value);
        if (productId.HasValue)
            sales = sales.Where(x => x.ProductId == productId.Value);
        if (categoryId.HasValue)
            sales = sales.Where(x =>
                x.Product.CategoryId == categoryId.Value);
        if (customerId.HasValue)
            sales = sales.Where(x =>
                x.SalesInvoice.CustomerId == customerId.Value);

        var saleTransactions = await sales
            .Select(x => new GrossProfitTransaction
            {
                Date = x.SalesInvoice.InvoiceDate,
                InvoiceId = x.SalesInvoiceId,
                InvoiceNumber = x.SalesInvoice.InvoiceNumber,
                ProductId = x.ProductId,
                ProductName = x.Product.Name,
                CategoryId = x.Product.CategoryId,
                CategoryName = x.Product.Category.Name,
                CustomerId = x.SalesInvoice.CustomerId,
                CustomerName = x.SalesInvoice.Customer.Name,
                Quantity = x.Quantity,
                SellingUnitPrice = x.SellingUnitPrice,
                CostAtSale = x.CostAtSale!.Value,
                IsReturn = false
            })
            .ToListAsync(cancellationToken);

        var returns = _context.CustomerReturnItems
            .AsNoTracking()
            .Where(x =>
                x.CustomerReturn.Status == CustomerReturnStatus.Posted &&
                x.CustomerReturn.SalesInvoice.Status !=
                    SalesInvoiceStatus.Cancelled);

        if (fromDate.HasValue)
            returns = returns.Where(x =>
                x.CustomerReturn.ReturnDate >= fromDate.Value);
        if (toDate.HasValue)
            returns = returns.Where(x =>
                x.CustomerReturn.ReturnDate <= toDate.Value);
        if (invoiceId.HasValue)
            returns = returns.Where(x =>
                x.CustomerReturn.SalesInvoiceId == invoiceId.Value);
        if (productId.HasValue)
            returns = returns.Where(x => x.ProductId == productId.Value);
        if (categoryId.HasValue)
            returns = returns.Where(x =>
                x.Product.CategoryId == categoryId.Value);
        if (customerId.HasValue)
            returns = returns.Where(x =>
                x.CustomerReturn.CustomerId == customerId.Value);

        var returnTransactions = await returns
            .Select(x => new GrossProfitTransaction
            {
                Date = x.CustomerReturn.ReturnDate,
                InvoiceId = x.CustomerReturn.SalesInvoiceId,
                InvoiceNumber = x.CustomerReturn.SalesInvoice.InvoiceNumber,
                ProductId = x.ProductId,
                ProductName = x.Product.Name,
                CategoryId = x.Product.CategoryId,
                CategoryName = x.Product.Category.Name,
                CustomerId = x.CustomerReturn.CustomerId,
                CustomerName = x.CustomerReturn.Customer.Name,
                Quantity = x.Quantity,
                SellingUnitPrice = x.SellingUnitPrice,
                CostAtSale = x.CostAtSale,
                IsReturn = true
            })
            .ToListAsync(cancellationToken);

        return saleTransactions
            .Concat(returnTransactions)
            .OrderBy(x => x.Date)
            .ToList();
    }
}
