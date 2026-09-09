using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using InventoryManagement.Application.Authorization;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.DTOs.User;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Identity;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ICompanyMembershipService _membershipService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(
            ICurrentUserService currentUserService,
            ICompanyMembershipService membershipService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _currentUserService = currentUserService;
            _membershipService = membershipService;
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var user = await FindCurrentUserAsync();
            var companies = await _membershipService.GetCompaniesForUserAsync(
                user.Id,
                HttpContext.RequestAborted);

            return Ok(new UserInfoDto
            {
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                IsDisabled = IsDisabled(user),
                Companies = companies.ToList()
            });
        }

        [HttpGet("admin-only")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public IActionResult AdminOnly()
        {
            return Ok("Welcome Owner");
        }

        [HttpGet("user-info")]
        public IActionResult UserInfo()
        {
            return Ok(new UserInfoDto
            {
                Username = _currentUserService.Username ?? ""
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
            var companyId = GetRequiredCompanyId();
            var memberships = await _context.CompanyUsers
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId)
                .OrderBy(x => x.UserId)
                .ToListAsync();

            var response = new List<UserManagementResponse>();

            foreach (var membership in memberships)
            {
                var user = await _userManager.FindByIdAsync(membership.UserId);

                if (user is not null)
                {
                    response.Add(MapUser(user, membership.Role));
                }
            }

            return Ok(response.OrderBy(x => x.UserName).ToList());
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> CreateUser(CreateUserRequest request)
        {
            await Task.CompletedTask;
            throw new BadRequestException(
                "Use company invitations to grant user access.");
        }

        [HttpPost("me/change-password")]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordRequest request)
        {
            if (request.NewPassword != request.ConfirmPassword)
            {
                throw new BadRequestException(
                    "New password and confirmation password do not match.");
            }

            var user = await FindCurrentUserAsync();
            var result = await _userManager.ChangePasswordAsync(
                user,
                request.CurrentPassword,
                request.NewPassword);

            if (!result.Succeeded)
            {
                throw new BadRequestException(
                    string.Join(",", result.Errors.Select(x => x.Description)));
            }

            return NoContent();
        }

        [HttpPost("{userId}/roles")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> AssignRole(
            string userId,
            AssignRoleRequest request)
        {
            var role = ValidateRole(request.Role);
            var user = await FindUserAsync(userId);
            var companyId = GetRequiredCompanyId();
            var membership = await _context.CompanyUsers
                .SingleOrDefaultAsync(x =>
                    x.CompanyId == companyId &&
                    x.UserId == user.Id);

            if (membership is null)
            {
                throw new NotFoundException(
                    "User is not a member of this company.");
            }

            await RequireCanManageMembershipAsync(
                companyId,
                membership.Role,
                role);

            if (membership.Role == CompanyRoles.Owner &&
                role != CompanyRoles.Owner &&
                await IsFinalActiveOwnerAsync(user, companyId))
            {
                throw new BadRequestException(
                    "Cannot change the final active Owner role.");
            }

            membership.Role = role;
            await _context.SaveChangesAsync();

            return Ok(MapUser(user, membership.Role));
        }

        [HttpDelete("{userId}/roles/{role}")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> RemoveRole(
            string userId,
            string role)
        {
            role = ValidateRole(role);
            var user = await FindUserAsync(userId);
            var companyId = GetRequiredCompanyId();
            await RequireCanManageMembershipAsync(
                companyId,
                currentRole: role,
                requestedRole: null);

            if (role == CompanyRoles.Owner &&
                await IsFinalActiveOwnerAsync(user, companyId))
            {
                throw new BadRequestException(
                    "Cannot remove the final active Owner role.");
            }

            var membership = await _context.CompanyUsers
                .SingleOrDefaultAsync(x =>
                    x.CompanyId == companyId &&
                    x.UserId == user.Id &&
                    x.Role == role);

            if (membership is not null)
            {
                _context.CompanyUsers.Remove(membership);
                await _context.SaveChangesAsync();
            }

            return Ok(MapUser(user, string.Empty));
        }

        [HttpPost("{userId}/disable")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> DisableUser(string userId)
        {
            var user = await FindUserAsync(userId);

            if (await IsFinalActiveOwnerAsync(user, GetRequiredCompanyId()))
            {
                throw new BadRequestException(
                    "Cannot disable the final active owner.");
            }

            var result = await _userManager.SetLockoutEndDateAsync(
                user,
                DateTimeOffset.MaxValue);

            if (!result.Succeeded)
            {
                throw new BadRequestException(
                    string.Join(",", result.Errors.Select(x => x.Description)));
            }

            return Ok(await MapUserForActiveCompanyAsync(user));
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

            return Ok(await MapUserForActiveCompanyAsync(user));
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

        private async Task<ApplicationUser> FindCurrentUserAsync()
        {
            var currentUserId =
                User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            ApplicationUser? user = null;

            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                user = await _userManager.FindByIdAsync(currentUserId);
            }

            if (user is null &&
                !string.IsNullOrWhiteSpace(_currentUserService.Username))
            {
                user = await _userManager.FindByNameAsync(
                    _currentUserService.Username);
            }

            if (user is null)
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            return user;
        }

        private static string ValidateRole(string role)
        {
            if (!CompanyRoles.All.Contains(role))
            {
                throw new BadRequestException(
                    "Role must be one of: " +
                    string.Join(", ", CompanyRoles.All));
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

        private async Task<bool> IsFinalActiveOwnerAsync(
            ApplicationUser user,
            int companyId)
        {
            if (IsDisabled(user))
            {
                return false;
            }

            var userMembership = await _context.CompanyUsers
                .AsNoTracking()
                .SingleOrDefaultAsync(x =>
                    x.CompanyId == companyId &&
                    x.UserId == user.Id);

            if (userMembership?.Role != CompanyRoles.Owner)
            {
                return false;
            }

            var ownerUserIds = await _context.CompanyUsers
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId &&
                            x.Role == CompanyRoles.Owner)
                .Select(x => x.UserId)
                .ToListAsync();

            var activeOwnerCount = 0;

            foreach (var ownerUserId in ownerUserIds)
            {
                var owner = await _userManager.FindByIdAsync(ownerUserId);

                if (owner is not null && !IsDisabled(owner))
                {
                    activeOwnerCount++;
                }
            }

            return activeOwnerCount <= 1;
        }

        private async Task RequireCanManageMembershipAsync(
            int companyId,
            string currentRole,
            string? requestedRole)
        {
            var currentUserId =
                User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            var currentMembership = await _membershipService
                .RequireCompanyRoleAsync(
                    currentUserId,
                    companyId,
                    [CompanyRoles.Owner, CompanyRoles.Admin],
                    HttpContext.RequestAborted);

            if ((IsPrivilegedRole(currentRole) ||
                 IsPrivilegedRole(requestedRole)) &&
                currentMembership.Role != CompanyRoles.Owner)
            {
                throw new ForbiddenException(
                    "Only an Owner can manage Owner and Admin access.");
            }
        }

        private async Task<UserManagementResponse> MapUserForActiveCompanyAsync(
            ApplicationUser user)
        {
            var companyId = GetRequiredCompanyId();
            var role = await _context.CompanyUsers
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId &&
                            x.UserId == user.Id)
                .Select(x => x.Role)
                .SingleOrDefaultAsync();

            return MapUser(user, role ?? string.Empty);
        }

        private static UserManagementResponse MapUser(
            ApplicationUser user,
            string role)
        {
            return new UserManagementResponse
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Roles = string.IsNullOrWhiteSpace(role)
                    ? new List<string>()
                    : new List<string> { role },
                IsDisabled = IsDisabled(user)
            };
        }

        private int GetRequiredCompanyId()
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyHeader) ||
                !int.TryParse(companyHeader.FirstOrDefault(), out var companyId))
            {
                throw new ForbiddenException("Active company is required.");
            }

            return companyId;
        }

        private static bool IsDisabled(ApplicationUser user)
        {
            return user.LockoutEnd.HasValue &&
                   user.LockoutEnd.Value > DateTimeOffset.UtcNow;
        }

        private static bool IsPrivilegedRole(string? role) =>
            role is CompanyRoles.Owner or CompanyRoles.Admin;
    }

    public class AssignRoleRequest
    {
        public string Role { get; set; } = string.Empty;
    }

    public class CreateUserRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
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
