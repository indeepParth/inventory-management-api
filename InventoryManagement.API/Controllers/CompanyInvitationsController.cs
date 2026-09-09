using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.API.Controllers
{
    [ApiController]
    [Route("api/company-invitations")]
    public class CompanyInvitationsController : ControllerBase
    {
        private const int InvitationLifetimeDays = 7;
        private const string CompanyIdHeaderName = "X-Company-Id";
        private readonly ApplicationDbContext _context;
        private readonly ICompanyMembershipService _membershipService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CompanyInvitationsController(
            ApplicationDbContext context,
            ICompanyMembershipService membershipService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _membershipService = membershipService;
            _userManager = userManager;
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> GetInvitations()
        {
            var companyId = GetRequiredCompanyId();
            var users = _context.Users.AsNoTracking();
            var invitations = await _context.CompanyInvitations
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new
                {
                    Invitation = x,
                    CompanyName = x.Company.Name,
                    InvitedByUserName = users
                        .Where(user => user.Id == x.InvitedByUserId)
                        .Select(user => user.UserName)
                        .SingleOrDefault()
                })
                .ToListAsync(HttpContext.RequestAborted);

            return Ok(invitations.Select(x => ToResponse(
                x.Invitation,
                x.CompanyName,
                x.InvitedByUserName ?? string.Empty)));
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> CreateInvitation(
            CreateCompanyInvitationRequest request)
        {
            var companyId = GetRequiredCompanyId();
            var invitedByUserId = GetCurrentUserId();
            var email = NormalizeEmail(request.Email);
            var normalizedEmail = NormalizeEmailKey(email);
            var role = ValidateRole(request.Role);
            await RequireCanGrantRoleAsync(invitedByUserId, companyId, role);

            var existingMemberUserId = await _context.Users
                .AsNoTracking()
                .Where(x => x.NormalizedEmail == normalizedEmail)
                .Select(x => x.Id)
                .SingleOrDefaultAsync(HttpContext.RequestAborted);

            if (!string.IsNullOrWhiteSpace(existingMemberUserId) &&
                await _context.CompanyUsers.AsNoTracking().AnyAsync(
                    x => x.CompanyId == companyId &&
                         x.UserId == existingMemberUserId,
                    HttpContext.RequestAborted))
            {
                throw new BadRequestException(
                    "A user with this email already has access to this company.");
            }

            var pendingInviteExists = await _context.CompanyInvitations
                .AsNoTracking()
                .AnyAsync(
                    x => x.CompanyId == companyId &&
                         x.NormalizedEmail == normalizedEmail &&
                         x.Status == CompanyInvitationStatuses.Pending,
                    HttpContext.RequestAborted);

            if (pendingInviteExists)
            {
                throw new BadRequestException(
                    "A pending invitation already exists for this email.");
            }

            var token = CreateInvitationToken();
            var now = DateTime.UtcNow;
            var invitation = new CompanyInvitation
            {
                CompanyId = companyId,
                Email = email,
                NormalizedEmail = normalizedEmail,
                Role = role,
                TokenHash = HashToken(token),
                Status = CompanyInvitationStatuses.Pending,
                InvitedByUserId = invitedByUserId,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddDays(InvitationLifetimeDays)
            };

            _context.CompanyInvitations.Add(invitation);
            await _context.SaveChangesAsync(HttpContext.RequestAborted);

            var companyName = await _context.Companies
                .AsNoTracking()
                .Where(x => x.Id == companyId)
                .Select(x => x.Name)
                .SingleAsync(HttpContext.RequestAborted);
            var invitedByUserName = User.Identity?.Name ?? string.Empty;

            return Created(
                $"/api/company-invitations/{invitation.Id}",
                ToCreateResponse(
                    invitation,
                    companyName,
                    invitedByUserName,
                    token));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> RevokeInvitation(int id)
        {
            var companyId = GetRequiredCompanyId();
            var invitation = await _context.CompanyInvitations
                .SingleOrDefaultAsync(
                    x => x.Id == id && x.CompanyId == companyId,
                    HttpContext.RequestAborted);

            if (invitation is null)
            {
                throw new NotFoundException("Invitation not found.");
            }

            if (GetEffectiveStatus(invitation) != CompanyInvitationStatuses.Pending)
            {
                throw new BadRequestException(
                    "Only pending invitations can be revoked.");
            }

            await RequireCanGrantRoleAsync(
                GetCurrentUserId(),
                companyId,
                invitation.Role);

            invitation.Status = CompanyInvitationStatuses.Revoked;
            invitation.RevokedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(HttpContext.RequestAborted);

            return NoContent();
        }

        [HttpGet("{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetInvitation(string token)
        {
            var invitation = await FindInvitationByTokenAsync(token);
            EnsureInvitationCanBeAccepted(invitation);

            return Ok(ToResponse(
                invitation,
                invitation.Company.Name,
                string.Empty));
        }

        [HttpPost("{token}/accept")]
        [AllowAnonymous]
        public async Task<IActionResult> AcceptInvitation(
            string token,
            AcceptInvitationRequest request)
        {
            var invitation = await FindInvitationByTokenAsync(token);
            EnsureInvitationCanBeAccepted(invitation);

            ApplicationUser user = request.Mode switch
            {
                "existingUser" => await GetMatchingCurrentUserAsync(invitation),
                "newUser" => await CreateInvitedUserAsync(invitation, request),
                _ => throw new BadRequestException("Invalid invitation accept mode.")
            };

            await using var transaction =
                await _context.Database.BeginTransactionAsync(HttpContext.RequestAborted);

            var membership = await _context.CompanyUsers
                .SingleOrDefaultAsync(
                    x => x.CompanyId == invitation.CompanyId &&
                         x.UserId == user.Id,
                    HttpContext.RequestAborted);

            if (membership is null)
            {
                _context.CompanyUsers.Add(new CompanyUser
                {
                    CompanyId = invitation.CompanyId,
                    UserId = user.Id,
                    Role = invitation.Role,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                membership.Role = invitation.Role;
            }

            invitation.Status = CompanyInvitationStatuses.Accepted;
            invitation.AcceptedByUserId = user.Id;
            invitation.AcceptedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(HttpContext.RequestAborted);
            await transaction.CommitAsync(HttpContext.RequestAborted);

            return Ok(new UserCompanyDto
            {
                Id = invitation.CompanyId,
                Name = invitation.Company.Name,
                Role = invitation.Role
            });
        }

        private async Task<CompanyInvitation> FindInvitationByTokenAsync(
            string token)
        {
            var tokenHash = HashToken(token);
            var invitation = await _context.CompanyInvitations
                .Include(x => x.Company)
                .SingleOrDefaultAsync(
                    x => x.TokenHash == tokenHash,
                    HttpContext.RequestAborted);

            if (invitation is null)
            {
                throw new NotFoundException("Invitation not found.");
            }

            return invitation;
        }

        private void EnsureInvitationCanBeAccepted(
            CompanyInvitation invitation)
        {
            if (GetEffectiveStatus(invitation) != CompanyInvitationStatuses.Pending)
            {
                throw new BadRequestException("Invitation is no longer valid.");
            }
        }

        private async Task<ApplicationUser> GetMatchingCurrentUserAsync(
            CompanyInvitation invitation)
        {
            var userId = GetCurrentUserIdOrNull();

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("Login is required.");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            if (NormalizeEmailKey(user.Email) != invitation.NormalizedEmail)
            {
                throw new BadRequestException(
                    "Invitation email does not match the current user.");
            }

            return user;
        }

        private async Task<ApplicationUser> CreateInvitedUserAsync(
            CompanyInvitation invitation,
            AcceptInvitationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserName))
            {
                throw new BadRequestException("Username is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new BadRequestException("Password is required.");
            }

            if (request.Password != request.ConfirmPassword)
            {
                throw new BadRequestException(
                    "Password and confirmation password do not match.");
            }

            var existingUser = await _userManager.FindByEmailAsync(
                invitation.Email);

            if (existingUser is not null)
            {
                throw new BadRequestException(
                    "A user with this email already exists. Login to accept this invitation.");
            }

            var user = new ApplicationUser
            {
                UserName = request.UserName.Trim(),
                Email = invitation.Email
            };

            var createResult = await _userManager.CreateAsync(
                user,
                request.Password);

            if (!createResult.Succeeded)
            {
                throw new BadRequestException(
                    string.Join(",", createResult.Errors.Select(x => x.Description)));
            }

            return user;
        }

        private async Task RequireCanGrantRoleAsync(
            string currentUserId,
            int companyId,
            string role)
        {
            var currentMembership = await _membershipService
                .RequireCompanyRoleAsync(
                    currentUserId,
                    companyId,
                    [CompanyRoles.Owner, CompanyRoles.Admin],
                    HttpContext.RequestAborted);

            if (IsPrivilegedRole(role) &&
                currentMembership.Role != CompanyRoles.Owner)
            {
                throw new ForbiddenException(
                    "Only an Owner can grant or remove Owner and Admin access.");
            }
        }

        private string GetCurrentUserId()
        {
            var userId = GetCurrentUserIdOrNull();

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            return userId;
        }

        private string? GetCurrentUserIdOrNull()
        {
            return User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                   User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private int GetRequiredCompanyId()
        {
            if (!Request.Headers.TryGetValue(CompanyIdHeaderName, out var companyHeader) ||
                !int.TryParse(companyHeader.FirstOrDefault(), out var companyId))
            {
                throw new ForbiddenException("Active company is required.");
            }

            return companyId;
        }

        private static CompanyInvitationResponse ToResponse(
            CompanyInvitation invitation,
            string companyName,
            string invitedByUserName)
        {
            return new CompanyInvitationResponse
            {
                Id = invitation.Id,
                CompanyId = invitation.CompanyId,
                CompanyName = companyName,
                Email = invitation.Email,
                Role = invitation.Role,
                Status = GetEffectiveStatus(invitation),
                InvitedByUserName = invitedByUserName,
                CreatedAtUtc = invitation.CreatedAtUtc,
                ExpiresAtUtc = invitation.ExpiresAtUtc,
                AcceptedAtUtc = invitation.AcceptedAtUtc
            };
        }

        private CompanyInvitationCreateResponse ToCreateResponse(
            CompanyInvitation invitation,
            string companyName,
            string invitedByUserName,
            string token)
        {
            var response = new CompanyInvitationCreateResponse
            {
                AcceptUrl = $"/accept-invitation/{token}"
            };

            var invitationResponse = ToResponse(
                invitation,
                companyName,
                invitedByUserName);

            response.Id = invitationResponse.Id;
            response.CompanyId = invitationResponse.CompanyId;
            response.CompanyName = invitationResponse.CompanyName;
            response.Email = invitationResponse.Email;
            response.Role = invitationResponse.Role;
            response.Status = invitationResponse.Status;
            response.InvitedByUserName = invitationResponse.InvitedByUserName;
            response.CreatedAtUtc = invitationResponse.CreatedAtUtc;
            response.ExpiresAtUtc = invitationResponse.ExpiresAtUtc;
            response.AcceptedAtUtc = invitationResponse.AcceptedAtUtc;

            return response;
        }

        private static string GetEffectiveStatus(CompanyInvitation invitation)
        {
            return invitation.Status == CompanyInvitationStatuses.Pending &&
                   invitation.ExpiresAtUtc <= DateTime.UtcNow
                ? CompanyInvitationStatuses.Expired
                : invitation.Status;
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

        private static bool IsPrivilegedRole(string role) =>
            role is CompanyRoles.Owner or CompanyRoles.Admin;

        private static string NormalizeEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new BadRequestException("Email is required.");
            }

            var normalized = email.Trim();

            try
            {
                _ = new MailAddress(normalized);
            }
            catch (FormatException)
            {
                throw new BadRequestException("Email must be valid.");
            }

            if (normalized.Length > 254)
            {
                throw new BadRequestException("Email must be 254 characters or fewer.");
            }

            return normalized;
        }

        private static string NormalizeEmailKey(string? email) =>
            (email ?? string.Empty).Trim().ToUpperInvariant();

        private static string CreateInvitationToken() =>
            WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

        private static string HashToken(string token) =>
            Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    public class CreateCompanyInvitationRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class AcceptInvitationRequest
    {
        public string Mode { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
    }

    public class CompanyInvitationResponse
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string InvitedByUserName { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? AcceptedAtUtc { get; set; }
    }

    public class CompanyInvitationCreateResponse : CompanyInvitationResponse
    {
        public string AcceptUrl { get; set; } = string.Empty;
    }
}
