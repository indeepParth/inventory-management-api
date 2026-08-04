using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Units.CreateUnit
{
    public class Handler : IRequestHandler<Command, InventoryManagement.Application.Features.Units.Response>
    {
        private readonly IUnitRepository _repository;

        public Handler(IUnitRepository repository)
        {
            _repository = repository;
        }

        public async Task<InventoryManagement.Application.Features.Units.Response> Handle(Command request, CancellationToken cancellationToken)
        {
            var name = request.Name.Trim();
            var existingUnit = await _repository.GetByNameAsync(name, cancellationToken);

            if (existingUnit is not null)
            {
                throw new BadRequestException("Unit name already exists.");
            }

            var unit = new InventoryManagement.Domain.Entities.Unit
            {
                Name = name,
                ShortName = string.IsNullOrWhiteSpace(request.ShortName)
                    ? null
                    : request.ShortName.Trim(),
                FactorToBaseUnit = request.FactorToBaseUnit,
                BaseUnitId = request.BaseUnitId,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            InventoryManagement.Domain.Entities.Unit? baseUnit = null;
            if (request.BaseUnitId.HasValue)
            {
                baseUnit = await _repository.GetByIdAsync(
                    request.BaseUnitId.Value,
                    cancellationToken);
                if (baseUnit is null)
                {
                    throw new NotFoundException("Base unit not found.");
                }

                if (!baseUnit.IsActive)
                {
                    throw new BadRequestException("Base unit is inactive.");
                }

                if (baseUnit.BaseUnitId != baseUnit.Id)
                {
                    throw new BadRequestException("Selected base unit must be a base unit.");
                }
            }

            await _repository.AddAsync(unit, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            if (!request.BaseUnitId.HasValue)
            {
                unit.BaseUnitId = unit.Id;
                await _repository.SaveChangesAsync(cancellationToken);
            }
            else
            {
                unit.BaseUnit = baseUnit;
            }

            return unit.ToResponse();
        }
    }
}
