using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Suppliers.UpdateSupplier
{
    public class Handler : IRequestHandler<Command, SupplierResponse>
    {
        private readonly ISupplierRepository _repository;

        public Handler(ISupplierRepository repository)
        {
            _repository = repository;
        }

        public async Task<SupplierResponse> Handle(Command request, CancellationToken cancellationToken)
        {
            var supplier = await _repository.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new NotFoundException("Supplier not found.");
            var name = request.Name.Trim();
            var gstNumber = SupplierMapping.NormalizeOptional(request.GstNumber)?.ToUpperInvariant();

            var sameName = await _repository.GetByNameAsync(name, cancellationToken);
            if (sameName is not null && sameName.Id != request.Id)
            {
                throw new BadRequestException("Supplier name already exists.");
            }

            if (gstNumber is not null)
            {
                var sameGst = await _repository.GetByGstNumberAsync(gstNumber, cancellationToken);
                if (sameGst is not null && sameGst.Id != request.Id)
                {
                    throw new BadRequestException("Supplier GST number already exists.");
                }
            }

            supplier.Name = name;
            supplier.ContactPerson = SupplierMapping.NormalizeOptional(request.ContactPerson);
            supplier.Email = SupplierMapping.NormalizeOptional(request.Email);
            supplier.Phone = SupplierMapping.NormalizeOptional(request.Phone);
            supplier.Address = SupplierMapping.NormalizeOptional(request.Address);
            supplier.GstNumber = gstNumber;
            supplier.IsActive = request.IsActive;

            await _repository.SaveChangesAsync(cancellationToken);
            return supplier.ToResponse();
        }
    }
}
