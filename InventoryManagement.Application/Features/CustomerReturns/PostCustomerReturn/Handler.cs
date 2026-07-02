using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.CustomerReturns.PostCustomerReturn
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
            CustomerReturn? posted = null;
            await _returns.ExecuteInTransactionAsync(async transactionToken =>
            {
                var customerReturn = await _returns.GetForUpdateAsync(
                    request.Id,
                    transactionToken) ??
                    throw new NotFoundException("Customer return not found.");
                if (customerReturn.Status == CustomerReturnStatus.Posted)
                {
                    posted = customerReturn;
                    return;
                }
                if (customerReturn.Status != CustomerReturnStatus.Draft)
                    throw new BadRequestException(
                        "Only Draft customer returns may be posted.");
                if (customerReturn.SalesInvoice.Status is
                    SalesInvoiceStatus.Draft or SalesInvoiceStatus.Cancelled)
                    throw new BadRequestException(
                        "The referenced sales invoice is no longer posted.");

                var itemIds = customerReturn.Items
                    .Select(x => x.SalesInvoiceItemId)
                    .ToList();
                var returned = await _returns.GetPostedReturnedQuantitiesAsync(
                    itemIds,
                    customerReturn.Id,
                    transactionToken);
                foreach (var item in customerReturn.Items)
                {
                    var remaining =
                        item.SalesInvoiceItem.Quantity -
                        returned.GetValueOrDefault(item.SalesInvoiceItemId);
                    if (item.Quantity > remaining)
                        throw new BadRequestException(
                            $"Return quantity exceeds the remaining returnable quantity for invoice item {item.SalesInvoiceItemId}.");
                }

                var now = DateTime.UtcNow;
                foreach (var item in customerReturn.Items)
                {
                    var product = item.Product;
                    var before = product.Quantity;
                    product.Quantity += item.Quantity;
                    await _movements.AddAsync(new StockMovement
                    {
                        ProductId = product.Id,
                        Product = product,
                        MovementType = StockMovementType.CustomerReturn,
                        QuantityChange = item.Quantity,
                        BalanceBefore = before,
                        BalanceAfter = product.Quantity,
                        UnitCost = item.CostAtSale,
                        SourceType = "CustomerReturn",
                        SourceId = customerReturn.Id.ToString(),
                        Reference = customerReturn.ReturnNumber,
                        OccurredAtUtc = now,
                        CreatedBy = _currentUser.Username
                    }, transactionToken);
                }

                customerReturn.Customer.BalanceDue -= customerReturn.GrandTotal;
                customerReturn.Customer.UpdatedAtUtc = now;
                customerReturn.Status = CustomerReturnStatus.Posted;
                customerReturn.PostedAtUtc = now;
                customerReturn.UpdatedAtUtc = now;
                await _returns.SaveChangesAsync(transactionToken);
                posted = customerReturn;
            }, cancellationToken);
            return posted!.ToResponse();
        }
    }
}
