using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Purchases.GetPurchaseById
{
    public class Handler : IRequestHandler<Query, PurchaseResponse>
    {
        private readonly IPurchaseRepository _repository;

        public Handler(IPurchaseRepository repository)
        {
            _repository = repository;
        }

        public async Task<PurchaseResponse> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var purchase = await _repository.GetByIdAsync(request.Id, cancellationToken);
            return purchase?.ToResponse() ??
                throw new NotFoundException("Purchase not found.");
        }
    }
}
