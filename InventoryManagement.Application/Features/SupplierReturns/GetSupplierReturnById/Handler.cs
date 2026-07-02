using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.SupplierReturns.GetSupplierReturnById
{
    public class Handler : IRequestHandler<Query, SupplierReturnResponse>
    {
        private readonly ISupplierReturnRepository _returns;

        public Handler(ISupplierReturnRepository returns)
        {
            _returns = returns;
        }

        public async Task<SupplierReturnResponse> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var supplierReturn = await _returns.GetByIdAsync(
                request.Id,
                cancellationToken) ??
                throw new NotFoundException("Supplier return not found.");
            return supplierReturn.ToResponse();
        }
    }
}
