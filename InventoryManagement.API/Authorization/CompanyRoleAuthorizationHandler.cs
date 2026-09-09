using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using InventoryManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace InventoryManagement.API.Authorization
{
    public class CompanyRoleAuthorizationHandler
        : AuthorizationHandler<CompanyRoleRequirement>
    {
        private const string CompanyIdHeaderName = "X-Company-Id";
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICompanyMembershipService _membershipService;

        public CompanyRoleAuthorizationHandler(
            IHttpContextAccessor httpContextAccessor,
            ICompanyMembershipService membershipService)
        {
            _httpContextAccessor = httpContextAccessor;
            _membershipService = membershipService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            CompanyRoleRequirement requirement)
        {
            var userId =
                context.User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext is null ||
                !httpContext.Request.Headers.TryGetValue(
                    CompanyIdHeaderName,
                    out var companyHeader) ||
                !int.TryParse(companyHeader.FirstOrDefault(), out var companyId))
            {
                return;
            }

            var hasAccess = await _membershipService.UserHasAnyRoleAsync(
                userId,
                companyId,
                requirement.AllowedRoles,
                httpContext.RequestAborted);

            if (hasAccess)
            {
                context.Succeed(requirement);
            }
        }
    }
}
