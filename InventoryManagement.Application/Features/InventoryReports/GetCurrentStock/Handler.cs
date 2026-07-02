using InventoryManagement.Application.Common.Models;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.InventoryReports.GetCurrentStock;

public class Handler : IRequestHandler<Query, PagedResponse<Response>>
{
    private readonly IProductRepository _repository;

    public Handler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResponse<Response>> Handle(
        Query request,
        CancellationToken cancellationToken)
    {
        var products = await _repository.GetCurrentStockReportAsync(
            request.PageNumber,
            request.PageSize,
            request.CategoryId,
            request.Search,
            request.Stock,
            cancellationToken);
        var totalCount = await _repository.GetCurrentStockReportCountAsync(
            request.CategoryId,
            request.Search,
            request.Stock,
            cancellationToken);

        return new PagedResponse<Response>
        {
            Items = products.Select(x => new Response
            {
                ProductId = x.Id,
                ProductName = x.Name,
                Category = x.Category.Name,
                Unit = x.BaseUnit,
                Quantity = x.Quantity,
                AverageCost = x.AverageCost,
                StockValue = x.Quantity * x.AverageCost,
                DefaultSellingPrice = x.DefaultSellingPrice
            }).ToList(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}
