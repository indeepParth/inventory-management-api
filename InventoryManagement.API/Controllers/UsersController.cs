using System.Security.Claims;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ICurrentUserService _currentUserService;

        public UsersController(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new UserInfoDto
            {
                Username = _currentUserService.Username ?? "",
                Roles = _currentUserService.Roles.ToList()
            });
        }

        [HttpGet("admin-only")]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminOnly()
        {
            return Ok("Wellcome Admin");
        }

        [HttpGet("user-info")]
        public IActionResult UserInfo()
        {
            return Ok(new UserInfoDto
            {
                Username = _currentUserService.Username ?? "",
                Roles = User.Claims
                .Where(x => x.Type == ClaimTypes.Role)
                .Select(x => x.Value)
                .ToList()
            });
        }

        [HttpGet("claims")]
        public IActionResult Claims()
        {
            return Ok(User.Claims.Select(x => new
            {
                x.Type,
                x.Value
            }));
        }
    }
}