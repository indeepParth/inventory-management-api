using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using MediatR;

namespace InventoryManagement.Application.Features.Customers.CreateCustomer
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
            var name = request.Name.Trim();
            var gstNumber = CustomerMapping.NormalizeOptional(request.GstNumber)?.ToUpperInvariant();

            if (await _repository.GetByNameAsync(name, cancellationToken) is not null)
            {
                throw new BadRequestException("Customer name already exists.");
            }

            if (gstNumber is not null &&
                await _repository.GetByGstNumberAsync(gstNumber, cancellationToken) is not null)
            {
                throw new BadRequestException("Customer GST number already exists.");
            }

            var now = DateTime.UtcNow;
            var customer = new Customer
            {
                Name = name,
                ContactPerson = CustomerMapping.NormalizeOptional(request.ContactPerson),
                Phone = CustomerMapping.NormalizeOptional(request.Phone),
                Email = CustomerMapping.NormalizeOptional(request.Email),
                BillingAddress = CustomerMapping.NormalizeOptional(request.BillingAddress),
                DeliveryAddress = CustomerMapping.NormalizeOptional(request.DeliveryAddress),
                GstNumber = gstNumber,
                CreditLimit = request.CreditLimit,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            await _repository.AddAsync(customer, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            return customer.ToResponse();
        }
    }
}
