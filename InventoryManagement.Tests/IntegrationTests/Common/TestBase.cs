using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using InventoryManagement.Application.Authorization;
using InventoryManagement.Application.DTOs.User;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Identity;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LoginCommand = InventoryManagement.Application.Features.Auth.Login.Command;
using LoginResponse = InventoryManagement.Application.Features.Auth.Login.Response;
using RegisterCommand = InventoryManagement.Application.Features.Auth.Register.Command;
using RegisterResponse = InventoryManagement.Application.Features.Auth.Register.Response;
using UnitResponse = InventoryManagement.Application.Features.Units.Response;

namespace InventoryManagement.Tests.IntegrationTests.Common
{
    public abstract class TestBase : IClassFixture<CustomWebApplicationFactory>
    {
        protected readonly HttpClient Client;
        private readonly CustomWebApplicationFactory _factory;
        protected int ActiveCompanyId { get; private set; }

        protected TestBase(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            Client = factory.CreateClient();
        }

        protected async Task AuthenticateAsync()
        {
            var unique = Guid.NewGuid().ToString("N");
            var username = $"test_{unique}";
            var password = "123456789";

            var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", new RegisterCommand
            {
                UserName = username,
                Email = $"{username}@user.com",
                Password = password
            });
            registerResponse.EnsureSuccessStatusCode();

            var register = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
            register.Should().NotBeNull();

            var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new LoginCommand
            {
                UserName = username,
                Password = password
            });

            loginResponse.EnsureSuccessStatusCode();

            var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
            login.Should().NotBeNull();

            Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", login!.AccessToken);
            Client.DefaultRequestHeaders.Remove("X-Company-Id");
            ActiveCompanyId = 0;
        }

        protected async Task<int> AuthenticateAndCreateCompanyAsync(
            string? companyName = null)
        {
            await AuthenticateAsync();

            var unique = Guid.NewGuid().ToString("N");
            var response = await Client.PostAsJsonAsync(
                "/api/companies",
                new
                {
                    Name = companyName ?? $"Test Company {unique}"
                });
            response.EnsureSuccessStatusCode();

            var company = await response.Content.ReadFromJsonAsync<UserCompanyDto>();
            company.Should().NotBeNull();

            SetActiveCompanyId(company!.Id);
            return company.Id;
        }

        protected async Task<int> GetUnitIdAsync(string name = "Piece")
        {
            var units = await Client.GetFromJsonAsync<List<UnitResponse>>("/api/units");
            units.Should().NotBeNull();

            var unit = units!.SingleOrDefault(x => x.Name == name);
            unit.Should().NotBeNull();

            return unit!.Id;
        }

        protected void SetActiveCompanyId(int companyId)
        {
            Client.DefaultRequestHeaders.Remove("X-Company-Id");
            Client.DefaultRequestHeaders.Add("X-Company-Id", companyId.ToString());
            ActiveCompanyId = companyId;
        }

        protected async Task SetActiveCompanyBillingPlanLimitsAsync(
            int maxCompanies = 10,
            int maxUsersPerCompany = 10,
            int maxInvoicesPerMonth = 1000,
            int maxProducts = 1000,
            int maxCustomers = 1000)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var company = await db.Companies
                .SingleAsync(x => x.Id == ActiveCompanyId);
            var plan = new SubscriptionPlan
            {
                Code = $"test-{Guid.NewGuid():N}",
                Name = "Test Plan",
                MaxCompanies = maxCompanies,
                MaxUsersPerCompany = maxUsersPerCompany,
                MaxInvoicesPerMonth = maxInvoicesPerMonth,
                MaxProducts = maxProducts,
                MaxCustomers = maxCustomers,
                IsActive = true
            };
            db.SubscriptionPlans.Add(plan);

            var subscription = await db.UserSubscriptions
                .SingleAsync(x => x.UserId == company.BillingOwnerUserId);
            subscription.Plan = plan;
            subscription.Status = SubscriptionStatuses.Active;
            subscription.ExpiresAtUtc = null;
            subscription.CancelledAtUtc = null;
            subscription.UpdatedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync();
        }
    }
}
