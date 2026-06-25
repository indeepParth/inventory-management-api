using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ISender _sender;

        public AuthController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(Application.Features.Auth.Register.Command request)
        {
            var responce = await _sender.Send(request);

            return Ok(responce);
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(Application.Features.Auth.Login.Command request)
        {
            var responce = await _sender.Send(request);

            return Ok(responce);
        }

        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken(Application.Features.Auth.RefreshAccessToken.Command request)
        {
            var responce = await _sender.Send(request);

            return Ok(responce);
        }
    }
}