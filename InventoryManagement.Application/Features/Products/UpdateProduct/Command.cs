using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Features.Products.UpdateProduct
{
    public sealed record Command(
        int Id,
        string Name,
        string SKU,
        UnitOfMeasure BaseUnit,
        decimal DefaultSellingPrice,
        int CategoryId,
        int? SupplierId
    ) : IRequest<Response>;
}
