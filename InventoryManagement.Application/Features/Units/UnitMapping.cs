using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Features.Units
{
    internal static class UnitMapping
    {
        public static Response ToResponse(this Unit unit) =>
            new()
            {
                Id = unit.Id,
                Name = unit.Name,
                ShortName = unit.ShortName,
                FactorToBaseUnit = unit.FactorToBaseUnit,
                BaseUnitId = unit.BaseUnitId,
                BaseUnitName = unit.BaseUnitId == unit.Id
                    ? unit.Name
                    : unit.BaseUnit?.Name,
                IsActive = unit.IsActive,
                CreatedAtUtc = unit.CreatedAtUtc
            };
    }
}
