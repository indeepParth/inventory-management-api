using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using InventoryManagement.Application.Authorization;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.DTOs.User;
using InventoryManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(
            ICurrentUserService currentUserService,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _currentUserService = currentUserService;
            _userManager = userManager;
            _roleManager = roleManager;
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
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public IActionResult AdminOnly()
        {
            return Ok("Welcome Admin");
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

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> GetUsers()
        {
            var users = _userManager.Users
                .OrderBy(x => x.UserName)
                .ToList();

            var response = new List<UserManagementResponse>();

            foreach (var user in users)
            {
                response.Add(await MapUserAsync(user));
            }

            return Ok(response);
        }

        [HttpPost("{userId}/roles")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> AssignRole(
            string userId,
            AssignRoleRequest request)
        {
            var role = ValidateRole(request.Role);
            var user = await FindUserAsync(userId);

            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }

            if (!await _userManager.IsInRoleAsync(user, role))
            {
                var result = await _userManager.AddToRoleAsync(user, role);

                if (!result.Succeeded)
                {
                    throw new BadRequestException(
                        string.Join(",", result.Errors.Select(x => x.Description)));
                }
            }

            return Ok(await MapUserAsync(user));
        }

        [HttpDelete("{userId}/roles/{role}")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> RemoveRole(
            string userId,
            string role)
        {
            role = ValidateRole(role);
            var user = await FindUserAsync(userId);

            if (role == ApplicationRoles.Admin &&
                IsCurrentUser(user) &&
                await IsFinalActiveAdminAsync(user))
            {
                throw new BadRequestException(
                    "Cannot remove your own final Admin role.");
            }

            if (await _userManager.IsInRoleAsync(user, role))
            {
                var result = await _userManager.RemoveFromRoleAsync(user, role);

                if (!result.Succeeded)
                {
                    throw new BadRequestException(
                        string.Join(",", result.Errors.Select(x => x.Description)));
                }
            }

            return Ok(await MapUserAsync(user));
        }

        [HttpPost("{userId}/disable")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> DisableUser(string userId)
        {
            var user = await FindUserAsync(userId);

            if (await IsFinalActiveAdminAsync(user))
            {
                throw new BadRequestException(
                    "Cannot disable the final active administrator.");
            }

            var result = await _userManager.SetLockoutEndDateAsync(
                user,
                DateTimeOffset.MaxValue);

            if (!result.Succeeded)
            {
                throw new BadRequestException(
                    string.Join(",", result.Errors.Select(x => x.Description)));
            }

            return Ok(await MapUserAsync(user));
        }

        [HttpPost("{userId}/enable")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> EnableUser(string userId)
        {
            var user = await FindUserAsync(userId);

            var result = await _userManager.SetLockoutEndDateAsync(
                user,
                null);

            if (!result.Succeeded)
            {
                throw new BadRequestException(
                    string.Join(",", result.Errors.Select(x => x.Description)));
            }

            return Ok(await MapUserAsync(user));
        }

        private async Task<ApplicationUser> FindUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }

            return user;
        }

        private static string ValidateRole(string role)
        {
            if (!ApplicationRoles.All.Contains(role))
            {
                throw new BadRequestException(
                    "Role must be one of: " +
                    string.Join(", ", ApplicationRoles.All));
            }

            return role;
        }

        private bool IsCurrentUser(ApplicationUser user)
        {
            var currentUserId =
                User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            return currentUserId == user.Id;
        }

        private async Task<bool> IsFinalActiveAdminAsync(ApplicationUser user)
        {
            if (!await _userManager.IsInRoleAsync(user, ApplicationRoles.Admin) ||
                IsDisabled(user))
            {
                return false;
            }

            var admins = await _userManager.GetUsersInRoleAsync(
                ApplicationRoles.Admin);

            return admins.Count(x => !IsDisabled(x)) <= 1;
        }

        private async Task<UserManagementResponse> MapUserAsync(
            ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            return new UserManagementResponse
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Roles = roles.OrderBy(x => x).ToList(),
                IsDisabled = IsDisabled(user)
            };
        }

        private static bool IsDisabled(ApplicationUser user)
        {
            return user.LockoutEnd.HasValue &&
                   user.LockoutEnd.Value > DateTimeOffset.UtcNow;
        }
    }

    public class AssignRoleRequest
    {
        public string Role { get; set; } = string.Empty;
    }

    public class UserManagementResponse
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public bool IsDisabled { get; set; }
    }
}
