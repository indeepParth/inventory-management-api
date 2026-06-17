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
        int Quantity,
        decimal Price
    ) : IRequest<Responce>;
    
    // public class Command : IRequest<Responce>
    // {
    //     internal int Id { get; set; }
    //     public string Name { get; set; } = string.Empty;
    //     public string SKU { get; set; } = string.Empty;
    //     public int Quantity { get; set; }
    //     public decimal Price { get; set; }
    // }
}