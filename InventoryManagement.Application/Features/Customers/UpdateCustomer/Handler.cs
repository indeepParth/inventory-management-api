using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Customers.UpdateCustomer
{
    public class Handler : IRequestHandler<Command, CustomerResponse>
    {
        private readonly ICustomerRepository _repository;

        public Handler(ICustomerRepository repository)
        {
            _repository = repository;
        }

        public async Task<CustomerResponse> Handle(Command request, CancellationToken cancellationToken)
        {
            var customer = await _repository.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new NotFoundException("Customer not found.");
            var name = request.Name.Trim();
            var gstNumber = CustomerMapping.NormalizeOptional(request.GstNumber)?.ToUpperInvariant();

            var sameName = await _repository.GetByNameAsync(name, cancellationToken);
            if (sameName is not null && sameName.Id != request.Id)
            {
                throw new BadRequestException("Customer name already exists.");
            }

            if (gstNumber is not null)
            {
                var sameGst = await _repository.GetByGstNumberAsync(gstNumber, cancellationToken);
                if (sameGst is not null && sameGst.Id != request.Id)
                {
                    throw new BadRequestException("Customer GST number already exists.");
                }
            }

            customer.Name = name;
            customer.ContactPerson = CustomerMapping.NormalizeOptional(request.ContactPerson);
            customer.Phone = CustomerMapping.NormalizeOptional(request.Phone);
            customer.Email = CustomerMapping.NormalizeOptional(request.Email);
            customer.BillingAddress = CustomerMapping.NormalizeOptional(request.BillingAddress);
            customer.DeliveryAddress = CustomerMapping.NormalizeOptional(request.DeliveryAddress);
            customer.GstNumber = gstNumber;
            customer.CreditLimit = request.CreditLimit;
            customer.IsActive = request.IsActive;
            customer.UpdatedAtUtc = DateTime.UtcNow;

            await _repository.SaveChangesAsync(cancellationToken);
            return customer.ToResponse();
        }
    }
}
