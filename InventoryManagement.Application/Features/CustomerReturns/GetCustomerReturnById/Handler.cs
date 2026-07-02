using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.CustomerReturns.GetCustomerReturnById
{
    public class Handler : IRequestHandler<Query, CustomerReturnResponse>
    {
        private readonly ICustomerReturnRepository _returns;

        public Handler(ICustomerReturnRepository returns)
        {
            _returns = returns;
        }

        public async Task<CustomerReturnResponse> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var customerReturn = await _returns.GetByIdAsync(
                request.Id,
                cancellationToken) ??
                throw new NotFoundException("Customer return not found.");
            return customerReturn.ToResponse();
        }
    }
}
