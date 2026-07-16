using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Drivers.GetDriverById
{
    public class Handler : IRequestHandler<Query, DriverResponse>
    {
        private readonly IDriverRepository _repository;

        public Handler(IDriverRepository repository)
        {
            _repository = repository;
        }

        public async Task<DriverResponse> Handle(Query request, CancellationToken cancellationToken)
        {
            var driver = await _repository.GetByIdAsync(request.Id, cancellationToken);
            return driver?.ToResponse() ?? throw new NotFoundException("Driver not found.");
        }
    }
}
