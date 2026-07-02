using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.InventoryReports.GetGrossProfit;

public class Handler : IRequestHandler<Query, Response>
{
    private readonly IGrossProfitReportRepository _repository;

    public Handler(IGrossProfitReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<Response> Handle(
        Query request,
        CancellationToken cancellationToken)
    {
        var transactions = await _repository.GetTransactionsAsync(
            request.FromDate,
            request.ToDate,
            request.InvoiceId,
            request.ProductId,
            request.CategoryId,
            request.CustomerId,
            cancellationToken);

        return new Response
        {
            Summary = Calculate(transactions),
            ByInvoice = transactions
                .GroupBy(x => new { x.InvoiceId, x.InvoiceNumber })
                .Select(x => Copy<InvoiceBreakdown>(Calculate(x), item =>
                {
                    item.InvoiceId = x.Key.InvoiceId;
                    item.InvoiceNumber = x.Key.InvoiceNumber;
                }))
                .OrderBy(x => x.InvoiceNumber)
                .ToList(),
            ByProduct = transactions
                .GroupBy(x => new { x.ProductId, x.ProductName })
                .Select(x => Copy<ProductBreakdown>(Calculate(x), item =>
                {
                    item.ProductId = x.Key.ProductId;
                    item.ProductName = x.Key.ProductName;
                }))
                .OrderBy(x => x.ProductName)
                .ToList(),
            ByCategory = transactions
                .GroupBy(x => new { x.CategoryId, x.CategoryName })
                .Select(x => Copy<CategoryBreakdown>(Calculate(x), item =>
                {
                    item.CategoryId = x.Key.CategoryId;
                    item.CategoryName = x.Key.CategoryName;
                }))
                .OrderBy(x => x.CategoryName)
                .ToList(),
            ByCustomer = transactions
                .GroupBy(x => new { x.CustomerId, x.CustomerName })
                .Select(x => Copy<CustomerBreakdown>(Calculate(x), item =>
                {
                    item.CustomerId = x.Key.CustomerId;
                    item.CustomerName = x.Key.CustomerName;
                }))
                .OrderBy(x => x.CustomerName)
                .ToList()
        };
    }

    private static GrossProfitValues Calculate(
        IEnumerable<GrossProfitTransaction> transactions)
    {
        var items = transactions.ToList();
        var sales = items.Where(x => !x.IsReturn).ToList();
        var returns = items.Where(x => x.IsReturn).ToList();
        var revenue = sales.Sum(x => x.Quantity * x.SellingUnitPrice);
        var returnRevenue = returns.Sum(x => x.Quantity * x.SellingUnitPrice);
        var cost = sales.Sum(x => x.Quantity * x.CostAtSale);
        var returnCost = returns.Sum(x => x.Quantity * x.CostAtSale);
        var netRevenue = revenue - returnRevenue;
        var netCost = cost - returnCost;
        var grossProfit = netRevenue - netCost;

        return new GrossProfitValues
        {
            SoldQuantity = sales.Sum(x => x.Quantity),
            ReturnedQuantity = returns.Sum(x => x.Quantity),
            NetQuantity = sales.Sum(x => x.Quantity) - returns.Sum(x => x.Quantity),
            Revenue = revenue,
            Returns = returnRevenue,
            NetRevenue = netRevenue,
            CostOfGoodsSold = cost,
            ReturnedCost = returnCost,
            NetCostOfGoodsSold = netCost,
            GrossProfit = grossProfit,
            GrossMarginPercentage = netRevenue == 0m
                ? 0m
                : decimal.Round(
                    grossProfit / netRevenue * 100m,
                    2,
                    MidpointRounding.AwayFromZero)
        };
    }

    private static T Copy<T>(GrossProfitValues source, Action<T> setKey)
        where T : GrossProfitValues, new()
    {
        var target = new T
        {
            SoldQuantity = source.SoldQuantity,
            ReturnedQuantity = source.ReturnedQuantity,
            NetQuantity = source.NetQuantity,
            Revenue = source.Revenue,
            Returns = source.Returns,
            NetRevenue = source.NetRevenue,
            CostOfGoodsSold = source.CostOfGoodsSold,
            ReturnedCost = source.ReturnedCost,
            NetCostOfGoodsSold = source.NetCostOfGoodsSold,
            GrossProfit = source.GrossProfit,
            GrossMarginPercentage = source.GrossMarginPercentage
        };
        setKey(target);
        return target;
    }
}
