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
        public IActionResult Register(Application.Features.Auth.Register.Command request)
        {
            var responce = _sender.Send(request);

            return Ok(responce);
        }

        [HttpPost("Login")]
        public IActionResult Login(Application.Features.Auth.Login.Command request)
        {
            var responce = _sender.Send(request);

            return Ok(responce);
        }

        [HttpPost("RefreshToken")]
        public IActionResult RefreshToken(Application.Features.Auth.RefreshAccessToken.Command request)
        {
            var responce = _sender.Send(request);

            return Ok(responce);
        }
    }
}