using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Suppliers.GetSupplierById
{
    public class Handler : IRequestHandler<Query, SupplierResponse>
    {
        private readonly ISupplierRepository _repository;

        public Handler(ISupplierRepository repository)
        {
            _repository = repository;
        }

        public async Task<SupplierResponse> Handle(Query request, CancellationToken cancellationToken)
        {
            var supplier = await _repository.GetByIdAsync(request.Id, cancellationToken);
            return supplier?.ToResponse() ?? throw new NotFoundException("Supplier not found.");
        }
    }
}
