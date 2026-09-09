using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using InventoryManagement.Application.Authorization;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.DTOs.Company;
using InventoryManagement.Application.DTOs.User;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CompaniesController : ControllerBase
    {
        private const int CompanyNameMaxLength = 150;
        private const string CompanyIdHeaderName = "X-Company-Id";
        private readonly ApplicationDbContext _context;
        private readonly ICompanyMembershipService _membershipService;

        public CompaniesController(
            ApplicationDbContext context,
            ICompanyMembershipService membershipService)
        {
            _context = context;
            _membershipService = membershipService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyCompanies()
        {
            var companies = await _membershipService.GetCompaniesForUserAsync(
                GetCurrentUserId(),
                HttpContext.RequestAborted);

            return Ok(companies);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCompany(CreateCompanyRequest request)
        {
            var name = ValidateCompanyName(request.Name);
            var userId = GetCurrentUserId();

            var company = new Company
            {
                Name = name,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.Companies.Add(company);
            _context.CompanyUsers.Add(new CompanyUser
            {
                Company = company,
                UserId = userId,
                Role = CompanyRoles.Owner,
                CreatedAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(HttpContext.RequestAborted);

            var response = new UserCompanyDto
            {
                Id = company.Id,
                Name = company.Name,
                Role = CompanyRoles.Owner
            };

            return Created($"/api/companies/{company.Id}", response);
        }

        [HttpPut("{companyId:int}")]
        public async Task<IActionResult> UpdateCompany(
            int companyId,
            UpdateCompanyRequest request)
        {
            var name = ValidateCompanyName(request.Name);
            var userId = GetCurrentUserId();
            var membership = await _membershipService.RequireCompanyRoleAsync(
                userId,
                companyId,
                [CompanyRoles.Owner, CompanyRoles.Admin],
                HttpContext.RequestAborted);

            var company = await _context.Companies
                .SingleOrDefaultAsync(x => x.Id == companyId, HttpContext.RequestAborted);

            if (company is null)
            {
                throw new NotFoundException("Company not found.");
            }

            company.Name = name;
            await _context.SaveChangesAsync(HttpContext.RequestAborted);

            return Ok(new UserCompanyDto
            {
                Id = company.Id,
                Name = company.Name,
                Role = membership.Role
            });
        }

        [HttpGet("current/access")]
        public async Task<IActionResult> GetCurrentCompanyAccess()
        {
            var companyId = GetRequiredCompanyId();
            var membership = await _membershipService.RequireCompanyAccessAsync(
                GetCurrentUserId(),
                companyId,
                HttpContext.RequestAborted);

            return Ok(new CompanyAccessDto
            {
                CompanyId = membership.Id,
                CompanyName = membership.Name,
                Role = membership.Role,
                Permissions = CompanyPermissions
                    .GetPermissionsForRole(membership.Role)
                    .ToList()
            });
        }

        private string GetCurrentUserId()
        {
            var userId =
                User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            return userId;
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

        private static string ValidateCompanyName(string name)
        {
            name = name.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new BadRequestException("Company name is required.");
            }

            if (name.Length > CompanyNameMaxLength)
            {
                throw new BadRequestException(
                    $"Company name must be {CompanyNameMaxLength} characters or fewer.");
            }

            return name;
        }
    }

    public class CreateCompanyRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateCompanyRequest
    {
        public string Name { get; set; } = string.Empty;
    }
}
