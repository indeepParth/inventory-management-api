using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Suppliers.DeleteSupplier
{
    public class Handler : IRequestHandler<Command, Response>
    {
        private readonly ISupplierRepository _repository;

        public Handler(ISupplierRepository repository)
        {
            _repository = repository;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            var supplier = await _repository.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new NotFoundException("Supplier not found.");

            if (await _repository.HasProductsAsync(request.Id, cancellationToken))
            {
                throw new BadRequestException("Supplier cannot be deleted because it has products.");
            }

            _repository.Delete(supplier);
            await _repository.SaveChangesAsync(cancellationToken);

            return new Response { Id = request.Id, Message = "Supplier deleted successfully." };
        }
    }
}
