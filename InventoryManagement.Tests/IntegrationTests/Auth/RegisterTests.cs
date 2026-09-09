using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Authorization;
using InventoryManagement.Application.Features.Auth.Register;
using InventoryManagement.Infrastructure.Identity;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryManagement.Tests.IntegrationTests.Auth
{
    public class RegisterTests : TestBase
    {
        private readonly CustomWebApplicationFactory _factory;

        public RegisterTests(CustomWebApplicationFactory factory) : base (factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Register_Should_Create_User()
        {
            // Arrange
            var unique = Guid.NewGuid().ToString("N");
            var request = new Command
            {
                UserName = $"test_{unique}",
                Email = $"test_{unique}@user.com",
                Password = "123456789"
            };

            // Act

            var response = await Client.PostAsJsonAsync("/api/auth/register", request);

            // Assert
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();

                throw new Exception(body);
            }
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result =
                await response.Content
                    .ReadFromJsonAsync<Response>();

            result.Should().NotBeNull();
            result.UserName.Should().Be(request.UserName);
            result.Email.Should().Be(request.Email);
            result.CompanyId.Should().BeGreaterThan(0);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByNameAsync(request.UserName);
            user.Should().NotBeNull();

            var company = await context.Companies.FindAsync(result.CompanyId);
            company.Should().NotBeNull();
            company!.Name.Should().Be($"{request.UserName}'s Company");
            company.BillingOwnerUserId.Should().Be(user!.Id);

            var membership = context.CompanyUsers
                .SingleOrDefault(x =>
                    x.CompanyId == result.CompanyId &&
                    x.UserId == user!.Id);
            membership.Should().NotBeNull();
            membership!.Role.Should().Be(CompanyRoles.Owner);

            var globalRoles = await userManager.GetRolesAsync(user!);
            globalRoles.Should().BeEmpty();

            var subscription = await context.UserSubscriptions
                .Include(x => x.Plan)
                .SingleOrDefaultAsync(x => x.UserId == user!.Id);
            subscription.Should().NotBeNull();
            subscription!.Status.Should().Be(SubscriptionStatuses.Active);
            subscription.Plan.Code.Should().Be(SubscriptionPlans.Free);
            subscription.Plan.MaxCompanies.Should().Be(1);
            subscription.Plan.MaxUsersPerCompany.Should().Be(3);
            subscription.Plan.MaxInvoicesPerMonth.Should().Be(100);
            subscription.Plan.MaxProducts.Should().Be(500);
            subscription.Plan.MaxCustomers.Should().Be(500);
        }

        [Fact]
        public async Task Registered_User_Should_Login_And_Receive_Company_Context()
        {
            var unique = Guid.NewGuid().ToString("N");
            var userName = $"test_{unique}";
            var password = "123456789";

            var registerResponse = await Client.PostAsJsonAsync(
                "/api/auth/register",
                new Command
                {
                    UserName = userName,
                    Email = $"{userName}@user.com",
                    Password = password
                });

            registerResponse.EnsureSuccessStatusCode();

            var loginResponse = await Client.PostAsJsonAsync(
                "/api/auth/login",
                new Application.Features.Auth.Login.Command
                {
                    UserName = userName,
                    Password = password
                });

            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var login = await loginResponse.Content
                .ReadFromJsonAsync<Application.Features.Auth.Login.Response>();
            login.Should().NotBeNull();

            Client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    login!.AccessToken);

            var meResponse = await Client.GetAsync("/api/users/me");

            meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await meResponse.Content.ReadAsStringAsync();
            body.Should().Contain(userName);
            body.Should().Contain($"{userName}'s Company");
            body.Should().Contain(CompanyRoles.Owner);
        }

        [Fact]
        public async Task Register_Should_Return_Forbidden_When_Public_Registration_Is_Disabled()
        {
            using var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, configuration) =>
                {
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Authentication:AllowPublicRegistration"] = "false"
                        });
                });
            }).CreateClient();

            var unique = Guid.NewGuid().ToString("N");
            var request = new Command
            {
                UserName = $"blocked_{unique}",
                Email = $"blocked_{unique}@user.com",
                Password = "123456789"
            };

            var response = await client.PostAsJsonAsync(
                "/api/auth/register",
                request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("statusCode");
            body.Should().Contain("traceId");
            body.Should().Contain("Public registration is disabled.");
            body.Should().NotContain(request.UserName);
            body.Should().NotContain(request.Email);
            body.Should().NotContain("already");
        }

        [Fact]
        public async Task Register_Should_Reject_Duplicate_Email()
        {
            // Arrange
            var request = new Command
            {
                UserName = $"test_{Guid.NewGuid():N}",
                Email = $"test_{Guid.NewGuid():N}@user.com",
                Password = "123456789"
            };

            // First Register
            await Client.PostAsJsonAsync("/api/auth/register", request);

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await response.Content.ReadAsStringAsync();

            body.Should().Contain("already");
        }

        [Fact]
        public async Task Register_Should_Reject_Weak_Password()
        {
            var request = new Command
            {
                UserName = $"test_{Guid.NewGuid():N}",
                Email = $"test_{Guid.NewGuid():N}@user.com",
                Password = "123"
            };

            var response = await Client.PostAsJsonAsync("/api/auth/register", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("errors");
            body.Should().Contain("Password");
        }

        [Fact]
        public async Task Register_Should_Reject_Empty_UserName()
        {
            var request = new Command
            {
                UserName = "",
                Email = $"test_{Guid.NewGuid():N}@user.com",
                Password = "123456789"
            };

            var response = await Client.PostAsJsonAsync("/api/auth/register", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("errors");
            body.Should().Contain("UserName");
        }
    }
}
