using InventoryManagement.Application.Common.Models;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.InventoryReports.GetProductStockLedger;

public class Handler : IRequestHandler<Query, PagedResponse<Response>>
{
    private readonly IStockMovementRepository _repository;

    public Handler(IStockMovementRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResponse<Response>> Handle(
        Query request,
        CancellationToken cancellationToken)
    {
        var movements = await _repository.GetAsync(
            request.PageNumber,
            request.PageSize,
            request.ProductId,
            request.MovementType,
            request.FromDate,
            request.ToDate,
            cancellationToken);
        var totalCount = await _repository.CountAsync(
            request.ProductId,
            request.MovementType,
            request.FromDate,
            request.ToDate,
            cancellationToken);

        return new PagedResponse<Response>
        {
            Items = movements.Select(x => new Response
            {
                Id = x.Id,
                OccurredAtUtc = x.OccurredAtUtc,
                MovementType = x.MovementType,
                SourceType = x.SourceType,
                SourceId = x.SourceId,
                SourceReference = x.Reference,
                QuantityChange = x.QuantityChange,
                RunningBalance = x.BalanceAfter
            }).ToList(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}
