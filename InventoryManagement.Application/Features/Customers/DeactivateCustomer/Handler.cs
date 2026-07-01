using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Customers.DeactivateCustomer
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

            if (!customer.IsActive)
            {
                return customer.ToResponse();
            }

            customer.IsActive = false;
            customer.UpdatedAtUtc = DateTime.UtcNow;

            await _repository.SaveChangesAsync(cancellationToken);
            return customer.ToResponse();
        }
    }
}
