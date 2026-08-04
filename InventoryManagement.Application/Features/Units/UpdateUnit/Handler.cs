using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Units.UpdateUnit
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
            var unit = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (unit is null)
            {
                throw new NotFoundException("Unit not found.");
            }

            var name = request.Name.Trim();
            var existingUnit = await _repository.GetByNameAsync(name, cancellationToken);

            if (existingUnit is not null && existingUnit.Id != request.Id)
            {
                throw new BadRequestException("Unit name already exists.");
            }

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

            unit.Name = name;
            unit.ShortName = string.IsNullOrWhiteSpace(request.ShortName)
                ? null
                : request.ShortName.Trim();
            unit.BaseUnitId = request.BaseUnitId ?? unit.Id;
            unit.FactorToBaseUnit = unit.BaseUnitId == unit.Id
                ? 1m
                : request.FactorToBaseUnit;
            unit.IsActive = request.IsActive;

            await _repository.SaveChangesAsync(cancellationToken);
            unit.BaseUnit = unit.BaseUnitId == unit.Id ? unit : baseUnit;

            return unit.ToResponse();
        }
    }
}
