using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using MediatR;

namespace InventoryManagement.Application.Features.Drivers.CreateDriver
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
            var name = request.Name.Trim();

            if (await _repository.GetByNameAsync(name, cancellationToken) is not null)
            {
                throw new BadRequestException("Driver name already exists.");
            }

            var now = DateTime.UtcNow;
            var driver = new Driver
            {
                Name = name,
                Phone = DriverMapping.NormalizeOptional(request.Phone),
                LicenseNumber = DriverMapping.NormalizeOptional(request.LicenseNumber),
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            await _repository.AddAsync(driver, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            return driver.ToResponse();
        }
    }
}
