using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Authorization;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Identity;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LoginCommand = InventoryManagement.Application.Features.Auth.Login.Command;
using LoginResponse = InventoryManagement.Application.Features.Auth.Login.Response;
using RegisterCommand = InventoryManagement.Application.Features.Auth.Register.Command;
using RegisterResponse = InventoryManagement.Application.Features.Auth.Register.Response;

namespace InventoryManagement.Tests.IntegrationTests.Companies
{
    public class CompanyEndpointsTests : TestBase
    {
        private readonly CustomWebApplicationFactory _factory;

        public CompanyEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateCompany_Should_Add_Current_User_As_Owner()
        {
            var user = await RegisterAndAuthenticateAsync();
            var companyName = $"Second company {Guid.NewGuid():N}";

            var response = await Client.PostAsJsonAsync(
                "/api/companies",
                new { Name = companyName });

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var created = await response.Content.ReadFromJsonAsync<CompanySummary>();
            created.Should().NotBeNull();
            created!.Name.Should().Be(companyName);
            created.Role.Should().Be(CompanyRoles.Owner);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var membership = await context.CompanyUsers.SingleOrDefaultAsync(x =>
                x.CompanyId == created.Id &&
                x.UserId == user.Id);

            membership.Should().NotBeNull();
            membership!.Role.Should().Be(CompanyRoles.Owner);
        }

        [Fact]
        public async Task GetCompanies_Should_Return_All_Current_User_Memberships()
        {
            var user = await RegisterAndAuthenticateAsync();
            var sharedCompany = await CreateCompanyWithMembershipAsync(
                user.Id,
                CompanyRoles.Viewer);

            var response = await Client.GetAsync("/api/companies");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var companies = await response.Content
                .ReadFromJsonAsync<List<CompanySummary>>();
            companies.Should().NotBeNull();
            companies!.Select(x => x.Id)
                .Should()
                .Contain([user.CompanyId, sharedCompany.Id]);
            companies.Single(x => x.Id == sharedCompany.Id)
                .Role.Should().Be(CompanyRoles.Viewer);
        }

        [Theory]
        [InlineData(CompanyRoles.Owner, HttpStatusCode.OK)]
        [InlineData(CompanyRoles.Admin, HttpStatusCode.OK)]
        [InlineData(CompanyRoles.Manager, HttpStatusCode.Forbidden)]
        [InlineData(CompanyRoles.Staff, HttpStatusCode.Forbidden)]
        [InlineData(CompanyRoles.Viewer, HttpStatusCode.Forbidden)]
        public async Task UpdateCompany_Should_Respect_Company_Role(
            string role,
            HttpStatusCode expectedStatusCode)
        {
            var user = await CreateUserWithCompanyAsync(role);
            await AuthenticateAsAsync(user.UserName, user.Password, user.CompanyId);

            var newName = $"Updated company {Guid.NewGuid():N}";
            var response = await Client.PutAsJsonAsync(
                $"/api/companies/{user.CompanyId}",
                new { Name = newName });

            response.StatusCode.Should().Be(expectedStatusCode);

            if (expectedStatusCode == HttpStatusCode.OK)
            {
                var updated = await response.Content.ReadFromJsonAsync<CompanySummary>();
                updated.Should().NotBeNull();
                updated!.Name.Should().Be(newName);
                updated.Role.Should().Be(role);
            }
        }

        [Fact]
        public async Task UpdateCompany_Should_Reject_Non_Member()
        {
            var user = await RegisterAndAuthenticateAsync();
            var otherCompany = await CreateCompanyWithMembershipAsync(
                user.Id,
                CompanyRoles.Viewer,
                includeRequestedUser: false);

            var response = await Client.PutAsJsonAsync(
                $"/api/companies/{otherCompany.Id}",
                new { Name = "Blocked update" });

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CurrentAccess_Should_Return_Role_And_Permissions()
        {
            var user = await CreateUserWithCompanyAsync(CompanyRoles.Admin);
            await AuthenticateAsAsync(user.UserName, user.Password, user.CompanyId);

            var response = await Client.GetAsync("/api/companies/current/access");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var access = await response.Content.ReadFromJsonAsync<CompanyAccess>();
            access.Should().NotBeNull();
            access!.CompanyId.Should().Be(user.CompanyId);
            access.CompanyName.Should().Be(user.CompanyName);
            access.Role.Should().Be(CompanyRoles.Admin);
            access.Permissions.Should().Contain(AuthorizationPolicies.AdminOnly);
            access.Permissions.Should().Contain(AuthorizationPolicies.ManageSalesInvoices);
        }

        [Fact]
        public async Task CurrentAccess_Should_Require_Valid_Member_Company_Header()
        {
            var user = await RegisterAndAuthenticateAsync();
            Client.DefaultRequestHeaders.Remove("X-Company-Id");

            var missingResponse = await Client.GetAsync("/api/companies/current/access");
            missingResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            Client.DefaultRequestHeaders.Add("X-Company-Id", "not-an-id");
            var invalidResponse = await Client.GetAsync("/api/companies/current/access");
            invalidResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            Client.DefaultRequestHeaders.Remove("X-Company-Id");
            Client.DefaultRequestHeaders.Add("X-Company-Id", (user.CompanyId + 9999).ToString());
            var nonMemberResponse = await Client.GetAsync("/api/companies/current/access");
            nonMemberResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CompanyUser_Should_Prevent_Duplicate_User_Company_Membership()
        {
            var user = await CreateUserWithCompanyAsync(CompanyRoles.Owner);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            context.CompanyUsers.Add(new CompanyUser
            {
                CompanyId = user.CompanyId,
                UserId = user.Id,
                Role = CompanyRoles.Viewer,
                CreatedAtUtc = DateTime.UtcNow
            });

            var act = async () => await context.SaveChangesAsync();

            await act.Should().ThrowAsync<DbUpdateException>();
        }

        private async Task<TestUser> RegisterAndAuthenticateAsync()
        {
            var unique = Guid.NewGuid().ToString("N");
            var userName = $"company_{unique}";
            const string password = "123456789";

            var registerResponse = await Client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterCommand
                {
                    UserName = userName,
                    Email = $"{userName}@example.com",
                    Password = password
                });

            registerResponse.EnsureSuccessStatusCode();

            var register = await registerResponse.Content
                .ReadFromJsonAsync<RegisterResponse>();
            register.Should().NotBeNull();

            await AuthenticateAsAsync(userName, password, register!.CompanyId);

            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByNameAsync(userName);
            user.Should().NotBeNull();

            return new TestUser(
                user!.Id,
                userName,
                password,
                register.CompanyId,
                $"{userName}'s Company");
        }

        private async Task<TestUser> CreateUserWithCompanyAsync(string role)
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var unique = Guid.NewGuid().ToString("N");
            var user = new ApplicationUser
            {
                UserName = $"company_user_{unique}",
                Email = $"company_user_{unique}@example.com"
            };
            const string password = "Password123";

            var createResult = await userManager.CreateAsync(user, password);
            createResult.Succeeded.Should().BeTrue();

            var company = new Company
            {
                Name = $"{user.UserName}'s Company",
                CreatedAtUtc = DateTime.UtcNow
            };

            context.Companies.Add(company);
            await context.SaveChangesAsync();

            context.CompanyUsers.Add(new CompanyUser
            {
                CompanyId = company.Id,
                UserId = user.Id,
                Role = role,
                CreatedAtUtc = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            return new TestUser(
                user.Id,
                user.UserName!,
                password,
                company.Id,
                company.Name);
        }

        private async Task<Company> CreateCompanyWithMembershipAsync(
            string requestedUserId,
            string requestedUserRole,
            bool includeRequestedUser = true)
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var owner = new ApplicationUser
            {
                UserName = $"company_owner_{Guid.NewGuid():N}",
                Email = $"company_owner_{Guid.NewGuid():N}@example.com"
            };

            var createResult = await userManager.CreateAsync(owner, "Password123");
            createResult.Succeeded.Should().BeTrue();

            var company = new Company
            {
                Name = $"Shared company {Guid.NewGuid():N}",
                CreatedAtUtc = DateTime.UtcNow
            };

            context.Companies.Add(company);
            await context.SaveChangesAsync();

            context.CompanyUsers.Add(new CompanyUser
            {
                CompanyId = company.Id,
                UserId = owner.Id,
                Role = CompanyRoles.Owner,
                CreatedAtUtc = DateTime.UtcNow
            });

            if (includeRequestedUser)
            {
                context.CompanyUsers.Add(new CompanyUser
                {
                    CompanyId = company.Id,
                    UserId = requestedUserId,
                    Role = requestedUserRole,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();

            return company;
        }

        private async Task AuthenticateAsAsync(
            string userName,
            string password,
            int companyId)
        {
            var response = await Client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginCommand
                {
                    UserName = userName,
                    Password = password
                });

            response.EnsureSuccessStatusCode();

            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
            login.Should().NotBeNull();

            Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", login!.AccessToken);
            Client.DefaultRequestHeaders.Remove("X-Company-Id");
            Client.DefaultRequestHeaders.Add("X-Company-Id", companyId.ToString());
        }

        private sealed record CompanySummary(int Id, string Name, string Role);

        private sealed record CompanyAccess(
            int CompanyId,
            string CompanyName,
            string Role,
            List<string> Permissions);

        private sealed record TestUser(
            string Id,
            string UserName,
            string Password,
            int CompanyId,
            string CompanyName);
    }
}
