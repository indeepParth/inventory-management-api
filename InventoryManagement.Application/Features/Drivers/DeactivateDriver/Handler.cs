using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Drivers.DeactivateDriver
{
    public class Handler : IRequestHandler<Command, DriverResponse>
    {
        private readonly IDriverRepository _repository;

        public Handler(IDriverRepository repository)
        {
            _repository = repository;
        }

        public async Task<DriverResponse> Handle(Command request, CancellationToken cancellationToken)
        {
            var driver = await _repository.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new NotFoundException("Driver not found.");

            if (!driver.IsActive)
            {
                return driver.ToResponse();
            }

            driver.IsActive = false;
            driver.UpdatedAtUtc = DateTime.UtcNow;

            await _repository.SaveChangesAsync(cancellationToken);
            return driver.ToResponse();
        }
    }
}
