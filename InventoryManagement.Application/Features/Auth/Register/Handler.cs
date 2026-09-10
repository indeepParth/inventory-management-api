using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Identity;
using MediatR;

namespace InventoryManagement.Application.Features.Auth.Register
{
    public class Handler : IRequestHandler<Command, Response>
    {
        private readonly IUserRegistrationService _registrationService;

        public Handler(IUserRegistrationService registrationService)
        {
            _registrationService = registrationService;
        }
        
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            var result = await _registrationService.RegisterAsync(
                request.UserName,
                request.Email,
                request.Password,
                cancellationToken);

            if (!result.Success)
            {
                throw new BadRequestException(string.Join(",", result.Errors));
            }

            return new Response
            {
                UserName = request.UserName,
                Email = request.Email
            };
        }
    }
}
