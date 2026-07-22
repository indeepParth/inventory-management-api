using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Authorization;
using InventoryManagement.Application.Features.CompanyProfile;
using InventoryManagement.Infrastructure.Identity;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using LoginCommand = InventoryManagement.Application.Features.Auth.Login.Command;
using LoginResponse = InventoryManagement.Application.Features.Auth.Login.Response;

namespace InventoryManagement.Tests.IntegrationTests.CompanyProfile
{
    public class CompanyProfileEndpointsTests : TestBase
    {
        private readonly CustomWebApplicationFactory _factory;

        public CompanyProfileEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Get_Should_Return_Empty_Profile_When_Not_Configured()
        {
            await ClearCompanyProfileAsync();
            await AuthenticateAsync();

            var profile = await Client.GetFromJsonAsync<CompanyProfileResponse>(
                "/api/company-profile");

            profile.Should().NotBeNull();
            profile!.CompanyName.Should().BeEmpty();
            profile.CreatedAtUtc.Should().BeNull();
            profile.UpdatedAtUtc.Should().BeNull();
        }

        [Fact]
        public async Task Put_Should_Upsert_And_Get_Should_Return_Profile()
        {
            await ClearCompanyProfileAsync();
            await AuthenticateAsync();

            var request = new
            {
                CompanyName = "StockFlow Trading",
                Address = "Main Road, Ahmedabad",
                GstNumber = "24ABCDE1234F1Z5",
                Email = "accounts@example.com",
                Phone = "9876543210",
                Website = "https://stockflow.example.com"
            };

            var updateResponse = await Client.PutAsJsonAsync(
                "/api/company-profile",
                request);

            updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var profile = await Client.GetFromJsonAsync<CompanyProfileResponse>(
                "/api/company-profile");

            profile.Should().NotBeNull();
            profile!.CompanyName.Should().Be("StockFlow Trading");
            profile.Address.Should().Be("Main Road, Ahmedabad");
            profile.GstNumber.Should().Be("24ABCDE1234F1Z5");
            profile.Email.Should().Be("accounts@example.com");
            profile.Phone.Should().Be("9876543210");
            profile.Website.Should().Be("https://stockflow.example.com");
            profile.CreatedAtUtc.Should().NotBeNull();
            profile.UpdatedAtUtc.Should().NotBeNull();
        }

        [Fact]
        public async Task Put_Should_Return_Validation_Error_For_Missing_Company_Name()
        {
            await ClearCompanyProfileAsync();
            await AuthenticateAsync();

            var response = await Client.PutAsJsonAsync(
                "/api/company-profile",
                new
                {
                    CompanyName = "",
                    Website = "not-a-url"
                });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("errors");
            body.Should().Contain("CompanyName");
            body.Should().Contain("Website");
        }

        [Fact]
        public async Task Non_Admin_Should_Not_Access_Company_Profile()
        {
            await ClearCompanyProfileAsync();
            await AuthenticateWithRoleAsync(ApplicationRoles.Manager);

            var getResponse = await Client.GetAsync("/api/company-profile");
            var putResponse = await Client.PutAsJsonAsync(
                "/api/company-profile",
                new { CompanyName = "Blocked" });

            getResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            putResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        private async Task ClearCompanyProfileAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            db.CompanyProfiles.RemoveRange(db.CompanyProfiles);
            await db.SaveChangesAsync();
        }

        private async Task AuthenticateWithRoleAsync(string role)
        {
            var user = await CreateUserAsync(role);

            var response = await Client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginCommand
                {
                    UserName = user.UserName,
                    Password = user.Password
                });

            response.EnsureSuccessStatusCode();

            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
            login.Should().NotBeNull();

            Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    login!.AccessToken);
        }

        private async Task<TestUser> CreateUserAsync(string role)
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

            foreach (var supportedRole in ApplicationRoles.All)
            {
                if (!await roleManager.RoleExistsAsync(supportedRole))
                {
                    var roleResult = await roleManager.CreateAsync(
                        new IdentityRole(supportedRole));
                    roleResult.Succeeded.Should().BeTrue();
                }
            }

            var unique = Guid.NewGuid().ToString("N");
            var user = new ApplicationUser
            {
                UserName = $"company_profile_{unique}",
                Email = $"company_profile_{unique}@example.com"
            };
            const string password = "Password123";

            var createResult = await userManager.CreateAsync(user, password);
            createResult.Succeeded.Should().BeTrue();

            var addRoleResult = await userManager.AddToRoleAsync(user, role);
            addRoleResult.Succeeded.Should().BeTrue();

            return new TestUser(user.UserName!, password);
        }

        private sealed record TestUser(
            string UserName,
            string Password);
    }
}
