using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace InventoryManagement.Application.Features.Auth.RefreshAccessToken
{    
    public class Command : IRequest<Response>
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}