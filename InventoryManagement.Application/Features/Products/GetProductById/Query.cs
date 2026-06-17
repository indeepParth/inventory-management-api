using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace InventoryManagement.Application.Features.Products.GetProductById
{
    public class Query : IRequest<Responce>
    {
        public int Id { get; set; }
    }
}