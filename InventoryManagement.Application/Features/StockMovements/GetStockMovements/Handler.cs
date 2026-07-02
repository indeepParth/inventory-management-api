using InventoryManagement.Application.Common.Models;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.StockMovements.GetStockMovements
{
    public class Handler : IRequestHandler<Query, PagedResponse<Response>>
    {
        private readonly IStockMovementRepository _repository;

        public Handler(IStockMovementRepository repository)
        {
            _repository = repository;
        }

        public async Task<PagedResponse<Response>> Handle(Query request, CancellationToken cancellationToken)
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
                    ProductId = x.ProductId,
                    ProductName = x.Product.Name,
                    MovementType = x.MovementType,
                    QuantityChange = x.QuantityChange,
                    BalanceBefore = x.BalanceBefore,
                    BalanceAfter = x.BalanceAfter,
                    UnitCost = x.UnitCost,
                    SourceType = x.SourceType,
                    SourceId = x.SourceId,
                    Reference = x.Reference,
                    Reason = x.Reason,
                    Note = x.Note,
                    OccurredAtUtc = x.OccurredAtUtc,
                    CreatedBy = x.CreatedBy
                }).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
