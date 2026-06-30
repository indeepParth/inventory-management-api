using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Identity;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Auth.Register
{
    public class Handler : IRequestHandler<Command, Response>
    {
        private readonly IIdentityService _identityService;

        public Handler(IIdentityService identityService)
        {
            _identityService = identityService;
        }
        
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            var result = await _identityService.CreateUserAsync(
                request.UserName,
                request.Email,
                request.Password
            );

            if (!result.success)
            {
                throw new BadRequestException(string.Join(",", result.error));
            }

            return new Response
            {
                UserName = request.UserName,
                Email = request.Email
            };
        }
    }
}
