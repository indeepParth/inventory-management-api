using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.CustomerReturns.CancelCustomerReturn
{
    public class Handler : IRequestHandler<Command, CustomerReturnResponse>
    {
        private readonly ICustomerReturnRepository _returns;
        private readonly IStockMovementRepository _movements;
        private readonly ICurrentUserService _currentUser;

        public Handler(
            ICustomerReturnRepository returns,
            IStockMovementRepository movements,
            ICurrentUserService currentUser)
        {
            _returns = returns;
            _movements = movements;
            _currentUser = currentUser;
        }

        public async Task<CustomerReturnResponse> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            CustomerReturn? cancelled = null;
            await _returns.ExecuteInTransactionAsync(async transactionToken =>
            {
                var customerReturn = await _returns.GetForUpdateAsync(
                    request.Id,
                    transactionToken) ??
                    throw new NotFoundException("Customer return not found.");
                if (customerReturn.Status == CustomerReturnStatus.Cancelled)
                {
                    cancelled = customerReturn;
                    return;
                }

                var now = DateTime.UtcNow;
                if (customerReturn.Status == CustomerReturnStatus.Posted)
                {
                    foreach (var group in customerReturn.Items.GroupBy(x =>
                        x.ProductId))
                    {
                        if (group.First().Product.Quantity <
                            group.Sum(x => x.Quantity))
                            throw new BadRequestException(
                                $"Insufficient stock to cancel the return for product {group.Key}.");
                    }

                    foreach (var item in customerReturn.Items)
                    {
                        var product = item.Product;
                        var before = product.Quantity;
                        product.Quantity -= item.Quantity;
                        await _movements.AddAsync(new StockMovement
                        {
                            ProductId = product.Id,
                            Product = product,
                            MovementType = StockMovementType.Reversal,
                            QuantityChange = -item.Quantity,
                            BalanceBefore = before,
                            BalanceAfter = product.Quantity,
                            UnitCost = item.CostAtSale,
                            SourceType = "CustomerReturnCancellation",
                            SourceId = customerReturn.Id.ToString(),
                            Reference = customerReturn.ReturnNumber,
                            OccurredAtUtc = now,
                            CreatedBy = _currentUser.Username
                        }, transactionToken);
                    }
                    customerReturn.Customer.BalanceDue +=
                        customerReturn.GrandTotal;
                    customerReturn.Customer.UpdatedAtUtc = now;
                }
                else if (customerReturn.Status != CustomerReturnStatus.Draft)
                    throw new BadRequestException(
                        "Customer return cannot be cancelled.");

                customerReturn.Status = CustomerReturnStatus.Cancelled;
                customerReturn.CancelledAtUtc = now;
                customerReturn.UpdatedAtUtc = now;
                await _returns.SaveChangesAsync(transactionToken);
                cancelled = customerReturn;
            }, cancellationToken);
            return cancelled!.ToResponse();
        }
    }
}
