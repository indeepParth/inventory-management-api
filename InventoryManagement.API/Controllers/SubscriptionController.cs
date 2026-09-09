using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.DTOs.Subscription;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/subscription")]
    public class SubscriptionController : ControllerBase
    {
        private const string CompanyIdHeaderName = "X-Company-Id";
        private readonly ApplicationDbContext _context;
        private readonly ICompanyMembershipService _membershipService;

        public SubscriptionController(
            ApplicationDbContext context,
            ICompanyMembershipService membershipService)
        {
            _context = context;
            _membershipService = membershipService;
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentSubscription()
        {
            var userId = GetCurrentUserId();
            var subscription = await _context.UserSubscriptions
                .AsNoTracking()
                .Include(x => x.Plan)
                .SingleOrDefaultAsync(
                    x => x.UserId == userId,
                    HttpContext.RequestAborted);

            if (subscription is null)
            {
                throw new ForbiddenException(
                    "An active subscription is required.");
            }

            var usage = new SubscriptionUsageDto
            {
                OwnedCompanies = await _context.Companies.CountAsync(
                    x => x.BillingOwnerUserId == userId,
                    HttpContext.RequestAborted)
            };

            var companyId = await GetOptionalAccessibleCompanyIdAsync(userId);
            if (companyId.HasValue)
            {
                var now = DateTime.UtcNow;
                var monthStart = new DateTime(
                    now.Year,
                    now.Month,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc);
                var nextMonthStart = monthStart.AddMonths(1);

                usage.CompanyUsers = await _context.CompanyUsers.CountAsync(
                    x => x.CompanyId == companyId.Value,
                    HttpContext.RequestAborted);
                usage.Products = await _context.Products.CountAsync(
                    x => x.CompanyId == companyId.Value,
                    HttpContext.RequestAborted);
                usage.Customers = await _context.Customers.CountAsync(
                    x => x.CompanyId == companyId.Value,
                    HttpContext.RequestAborted);
                usage.InvoicesThisMonth = await _context.SalesInvoices.CountAsync(
                    x => x.CompanyId == companyId.Value &&
                         x.CreatedAtUtc >= monthStart &&
                         x.CreatedAtUtc < nextMonthStart,
                    HttpContext.RequestAborted);
            }

            return Ok(new CurrentSubscriptionDto
            {
                Status = subscription.Status,
                Plan = new SubscriptionPlanDto
                {
                    Code = subscription.Plan.Code,
                    Name = subscription.Plan.Name,
                    MaxCompanies = subscription.Plan.MaxCompanies,
                    MaxUsersPerCompany = subscription.Plan.MaxUsersPerCompany,
                    MaxInvoicesPerMonth = subscription.Plan.MaxInvoicesPerMonth,
                    MaxProducts = subscription.Plan.MaxProducts,
                    MaxCustomers = subscription.Plan.MaxCustomers
                },
                Usage = usage
            });
        }

        private async Task<int?> GetOptionalAccessibleCompanyIdAsync(
            string userId)
        {
            if (!Request.Headers.TryGetValue(CompanyIdHeaderName, out var companyHeader) ||
                string.IsNullOrWhiteSpace(companyHeader.FirstOrDefault()))
            {
                return null;
            }

            if (!int.TryParse(companyHeader.FirstOrDefault(), out var companyId))
            {
                throw new ForbiddenException("Active company is invalid.");
            }

            await _membershipService.RequireCompanyAccessAsync(
                userId,
                companyId,
                HttpContext.RequestAborted);

            return companyId;
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
    }
}
