using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using MediatR;

namespace InventoryManagement.Application.Features.Suppliers.CreateSupplier
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
            var name = request.Name.Trim();
            var gstNumber = SupplierMapping.NormalizeOptional(request.GstNumber)?.ToUpperInvariant();

            if (await _repository.GetByNameAsync(name, cancellationToken) is not null)
            {
                throw new BadRequestException("Supplier name already exists.");
            }

            if (gstNumber is not null &&
                await _repository.GetByGstNumberAsync(gstNumber, cancellationToken) is not null)
            {
                throw new BadRequestException("Supplier GST number already exists.");
            }

            var supplier = new Supplier
            {
                Name = name,
                ContactPerson = SupplierMapping.NormalizeOptional(request.ContactPerson),
                Email = SupplierMapping.NormalizeOptional(request.Email),
                Phone = SupplierMapping.NormalizeOptional(request.Phone),
                Address = SupplierMapping.NormalizeOptional(request.Address),
                GstNumber = gstNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(supplier, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            return supplier.ToResponse();
        }
    }
}
