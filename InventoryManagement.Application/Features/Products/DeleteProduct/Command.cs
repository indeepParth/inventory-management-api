using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace InventoryManagement.Application.Features.Products.DeleteProduct
{
    public class Command : IRequest<Response>
    {
        public int Id { get; set; }
    }
}