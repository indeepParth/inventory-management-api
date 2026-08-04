using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Features.ProductUnitConversions
{
    public static class ProductUnitConversionMapping
    {
        public static Response ToResponse(
            ProductUnitConversion conversion,
            int baseUnitId)
        {
            return new Response
            {
                Id = conversion.Id,
                ProductId = conversion.ProductId,
                UnitId = conversion.UnitId,
                UnitName = conversion.Unit.Name,
                FactorToBaseUnit = conversion.FactorToBaseUnit,
                IsActive = conversion.IsActive,
                IsBaseUnit = conversion.UnitId == baseUnitId
            };
        }
    }
}
