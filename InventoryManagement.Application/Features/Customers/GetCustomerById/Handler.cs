using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Customers.GetCustomerById
{
    public class Handler : IRequestHandler<Query, CustomerResponse>
    {
        private readonly ICustomerRepository _repository;

        public Handler(ICustomerRepository repository)
        {
            _repository = repository;
        }

        public async Task<CustomerResponse> Handle(Query request, CancellationToken cancellationToken)
        {
            var customer = await _repository.GetByIdAsync(request.Id, cancellationToken);
            return customer?.ToResponse() ?? throw new NotFoundException("Customer not found.");
        }
    }
}
