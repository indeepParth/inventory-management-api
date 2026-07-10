using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Options;
using MediatR;
using InventoryManagement.API.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace InventoryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly AuthenticationOptions _authenticationOptions;

        public AuthController(
            ISender sender,
            IOptions<AuthenticationOptions> authenticationOptions)
        {
            _sender = sender;
            _authenticationOptions = authenticationOptions.Value;
        }

        [HttpPost("Register")]
        [EnableRateLimiting(RateLimitPolicyNames.Register)]
        public async Task<IActionResult> Register(Application.Features.Auth.Register.Command request)
        {
            if (!_authenticationOptions.AllowPublicRegistration)
            {
                throw new ForbiddenException(
                    "Public registration is disabled.");
            }

            var response = await _sender.Send(request);

            return Ok(response);
        }

        [HttpPost("Login")]
        [EnableRateLimiting(RateLimitPolicyNames.Login)]
        public async Task<IActionResult> Login(Application.Features.Auth.Login.Command request)
        {
            var response = await _sender.Send(request);

            return Ok(response);
        }

        [HttpPost("RefreshToken")]
        [EnableRateLimiting(RateLimitPolicyNames.RefreshToken)]
        public async Task<IActionResult> RefreshToken(Application.Features.Auth.RefreshAccessToken.Command request)
        {
            var response = await _sender.Send(request);

            return Ok(response);
        }
    }
}
