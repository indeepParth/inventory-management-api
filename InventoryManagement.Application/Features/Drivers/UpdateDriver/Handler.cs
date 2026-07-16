using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Drivers.UpdateDriver
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
            var name = request.Name.Trim();

            var sameName = await _repository.GetByNameAsync(name, cancellationToken);
            if (sameName is not null && sameName.Id != request.Id)
            {
                throw new BadRequestException("Driver name already exists.");
            }

            driver.Name = name;
            driver.Phone = DriverMapping.NormalizeOptional(request.Phone);
            driver.LicenseNumber = DriverMapping.NormalizeOptional(request.LicenseNumber);
            driver.IsActive = request.IsActive;
            driver.UpdatedAtUtc = DateTime.UtcNow;

            await _repository.SaveChangesAsync(cancellationToken);
            return driver.ToResponse();
        }
    }
}
