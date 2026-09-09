using InventoryManagement.Application.Authorization;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Services
{
    public class SubscriptionProvisioningService :
        ISubscriptionProvisioningService,
        ISubscriptionLimitService
    {
        private readonly ApplicationDbContext _context;
        private readonly IActiveCompanyService _activeCompany;

        public SubscriptionProvisioningService(
            ApplicationDbContext context,
            IActiveCompanyService activeCompany)
        {
            _context = context;
            _activeCompany = activeCompany;
        }

        public async Task EnsureFreeSubscriptionAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (await _context.UserSubscriptions.AnyAsync(
                    x => x.UserId == userId,
                    cancellationToken))
            {
                return;
            }

            var plan = await GetFreePlanAsync(cancellationToken);
            var now = DateTime.UtcNow;
            _context.UserSubscriptions.Add(new UserSubscription
            {
                UserId = userId,
                PlanId = plan.Id,
                Status = SubscriptionStatuses.Active,
                StartedAtUtc = now,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        public async Task EnsureCanCreateCompanyAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            var subscription = await GetActiveSubscriptionAsync(
                userId,
                cancellationToken);
            var ownedCompanyCount = await _context.Companies.CountAsync(
                x => x.BillingOwnerUserId == userId,
                cancellationToken);

            if (ownedCompanyCount >= subscription.Plan.MaxCompanies)
            {
                throw new ForbiddenException(
                    $"Your current plan allows up to {subscription.Plan.MaxCompanies} company.");
            }
        }

        public async Task EnsureCanAddCompanyMemberAsync(
            int companyId,
            CancellationToken cancellationToken = default)
        {
            var subscription = await GetCompanyBillingSubscriptionAsync(
                companyId,
                cancellationToken);
            var memberCount = await _context.CompanyUsers.CountAsync(
                x => x.CompanyId == companyId,
                cancellationToken);

            if (memberCount >= subscription.Plan.MaxUsersPerCompany)
            {
                throw new ForbiddenException(
                    $"This company's billing plan allows up to {subscription.Plan.MaxUsersPerCompany} users.");
            }
        }

        public async Task EnsureCanInviteCompanyMemberAsync(
            int companyId,
            CancellationToken cancellationToken = default)
        {
            var subscription = await GetCompanyBillingSubscriptionAsync(
                companyId,
                cancellationToken);
            var now = DateTime.UtcNow;
            var reservedSeats = await _context.CompanyUsers.CountAsync(
                    x => x.CompanyId == companyId,
                    cancellationToken) +
                await _context.CompanyInvitations.CountAsync(
                    x => x.CompanyId == companyId &&
                         x.Status == CompanyInvitationStatuses.Pending &&
                         x.ExpiresAtUtc > now,
                    cancellationToken);

            if (reservedSeats >= subscription.Plan.MaxUsersPerCompany)
            {
                throw new ForbiddenException(
                    $"This company's billing plan allows up to {subscription.Plan.MaxUsersPerCompany} users.");
            }
        }

        public async Task EnsureCanCreateProductAsync(
            CancellationToken cancellationToken = default)
        {
            var companyId = _activeCompany.CompanyId;
            var subscription = await GetCompanyBillingSubscriptionAsync(
                companyId,
                cancellationToken);
            var productCount = await _context.Products.CountAsync(
                x => x.CompanyId == companyId,
                cancellationToken);

            if (productCount >= subscription.Plan.MaxProducts)
            {
                throw new ForbiddenException(
                    $"This company's billing plan allows up to {subscription.Plan.MaxProducts} products.");
            }
        }

        public async Task EnsureCanCreateCustomerAsync(
            CancellationToken cancellationToken = default)
        {
            var companyId = _activeCompany.CompanyId;
            var subscription = await GetCompanyBillingSubscriptionAsync(
                companyId,
                cancellationToken);
            var customerCount = await _context.Customers.CountAsync(
                x => x.CompanyId == companyId,
                cancellationToken);

            if (customerCount >= subscription.Plan.MaxCustomers)
            {
                throw new ForbiddenException(
                    $"This company's billing plan allows up to {subscription.Plan.MaxCustomers} customers.");
            }
        }

        public async Task EnsureCanCreateSalesInvoiceAsync(
            CancellationToken cancellationToken = default)
        {
            var companyId = _activeCompany.CompanyId;
            var subscription = await GetCompanyBillingSubscriptionAsync(
                companyId,
                cancellationToken);
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
            var invoiceCount = await _context.SalesInvoices.CountAsync(
                x => x.CompanyId == companyId &&
                     x.CreatedAtUtc >= monthStart &&
                     x.CreatedAtUtc < nextMonthStart,
                cancellationToken);

            if (invoiceCount >= subscription.Plan.MaxInvoicesPerMonth)
            {
                throw new ForbiddenException(
                    $"This company's billing plan allows up to {subscription.Plan.MaxInvoicesPerMonth} sales invoices per month.");
            }
        }

        private async Task<SubscriptionPlan> GetFreePlanAsync(
            CancellationToken cancellationToken)
        {
            return await _context.SubscriptionPlans.SingleOrDefaultAsync(
                    x => x.Code == SubscriptionPlans.Free,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "The Free subscription plan is missing.");
        }

        private async Task<UserSubscription> GetCompanyBillingSubscriptionAsync(
            int companyId,
            CancellationToken cancellationToken)
        {
            var billingOwnerUserId = await _context.Companies
                .Where(x => x.Id == companyId)
                .Select(x => x.BillingOwnerUserId)
                .SingleOrDefaultAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(billingOwnerUserId))
            {
                throw new ForbiddenException(
                    "Company billing owner is not configured.");
            }

            return await GetActiveSubscriptionAsync(
                billingOwnerUserId,
                cancellationToken);
        }

        private async Task<UserSubscription> GetActiveSubscriptionAsync(
            string userId,
            CancellationToken cancellationToken)
        {
            var subscription = await _context.UserSubscriptions
                .Include(x => x.Plan)
                .SingleOrDefaultAsync(
                    x => x.UserId == userId,
                    cancellationToken);

            if (subscription is null ||
                !subscription.Plan.IsActive ||
                !SubscriptionStatuses.ActiveStatuses.Contains(subscription.Status) ||
                (subscription.ExpiresAtUtc.HasValue &&
                 subscription.ExpiresAtUtc.Value <= DateTime.UtcNow))
            {
                throw new ForbiddenException(
                    "An active subscription is required.");
            }

            return subscription;
        }
    }
}
