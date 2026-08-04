using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace InventoryManagement.Application.Features.Products.UpdateProduct
{
    public sealed record Command(
        int Id,
        string Name,
        string SKU,
        int BaseUnitId,
        decimal DefaultSellingPrice,
        int CategoryId
    ) : IRequest<Response>;
}
