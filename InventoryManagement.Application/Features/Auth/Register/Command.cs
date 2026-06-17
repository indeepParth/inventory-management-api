using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace InventoryManagement.Application.Features.Auth.Register
{    
    public class Command : IRequest<Responce>
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Passward { get; set; } = string.Empty;
    }
}