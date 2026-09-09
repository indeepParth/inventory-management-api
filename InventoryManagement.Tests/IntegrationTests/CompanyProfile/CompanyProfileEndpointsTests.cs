using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Authorization;
using InventoryManagement.Application.DTOs.User;
using InventoryManagement.Application.Features.CompanyProfile;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Identity;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        public async Task Owner_And_Admin_Should_Access_Company_Profile()
        {
            await AuthenticateAsync();

            var ownerGetResponse = await Client.GetAsync("/api/company-profile");
            ownerGetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            await AuthenticateWithRoleAsync(CompanyRoles.Admin);

            var adminPutResponse = await Client.PutAsJsonAsync(
                "/api/company-profile",
                new { CompanyName = "Admin Editable Profile" });
            var adminGetResponse = await Client.GetAsync("/api/company-profile");

            adminPutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            adminGetResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Theory]
        [InlineData(CompanyRoles.Manager)]
        [InlineData(CompanyRoles.Staff)]
        [InlineData(CompanyRoles.Viewer)]
        public async Task Non_Admin_Roles_Should_Not_Access_Company_Profile(
            string role)
        {
            await AuthenticateWithRoleAsync(role);

            var getResponse = await Client.GetAsync("/api/company-profile");
            var putResponse = await Client.PutAsJsonAsync(
                "/api/company-profile",
                new { CompanyName = "Blocked" });

            getResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            putResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Missing_Or_Invalid_Company_Header_Should_Be_Forbidden()
        {
            await AuthenticateAsync();

            Client.DefaultRequestHeaders.Remove("X-Company-Id");

            var missingGetResponse = await Client.GetAsync("/api/company-profile");
            var missingPutResponse = await Client.PutAsJsonAsync(
                "/api/company-profile",
                new { CompanyName = "Missing Header" });

            Client.DefaultRequestHeaders.Add("X-Company-Id", "not-a-number");

            var invalidGetResponse = await Client.GetAsync("/api/company-profile");
            var invalidPutResponse = await Client.PutAsJsonAsync(
                "/api/company-profile",
                new { CompanyName = "Invalid Header" });

            missingGetResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            missingPutResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            invalidGetResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            invalidPutResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Non_Member_Should_Not_Access_Company_Profile()
        {
            await AuthenticateAsync();
            var otherCompanyId = await CreateCompanyWithoutMembershipAsync();

            SetActiveCompanyId(otherCompanyId);

            var getResponse = await Client.GetAsync("/api/company-profile");
            var putResponse = await Client.PutAsJsonAsync(
                "/api/company-profile",
                new { CompanyName = "Blocked" });

            getResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            putResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Profile_Should_Be_Isolated_By_Active_Company()
        {
            await AuthenticateAsync();
            var firstCompanyId = ActiveCompanyId;

            var firstProfileResponse = await Client.PutAsJsonAsync(
                "/api/company-profile",
                new
                {
                    CompanyName = "First Company Profile",
                    Address = "First address",
                    GstNumber = "24AAAAA1111A1Z5",
                    Email = "first@example.com",
                    Phone = "1111111111",
                    Website = "https://first.example.com"
                });
            firstProfileResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var createCompanyResponse = await Client.PostAsJsonAsync(
                "/api/companies",
                new { Name = "Second Company" });
            createCompanyResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var secondCompany = await createCompanyResponse.Content
                .ReadFromJsonAsync<UserCompanyDto>();
            secondCompany.Should().NotBeNull();

            SetActiveCompanyId(secondCompany!.Id);

            var emptySecondProfile = await Client
                .GetFromJsonAsync<CompanyProfileResponse>("/api/company-profile");
            emptySecondProfile.Should().NotBeNull();
            emptySecondProfile!.CompanyName.Should().BeEmpty();

            var secondProfileResponse = await Client.PutAsJsonAsync(
                "/api/company-profile",
                new
                {
                    CompanyName = "Second Company Profile",
                    Address = "Second address",
                    GstNumber = "24BBBBB2222B1Z5",
                    Email = "second@example.com",
                    Phone = "2222222222",
                    Website = "https://second.example.com"
                });
            secondProfileResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            SetActiveCompanyId(firstCompanyId);
            var firstProfile = await Client
                .GetFromJsonAsync<CompanyProfileResponse>("/api/company-profile");

            SetActiveCompanyId(secondCompany.Id);
            var secondProfile = await Client
                .GetFromJsonAsync<CompanyProfileResponse>("/api/company-profile");

            firstProfile.Should().NotBeNull();
            firstProfile!.CompanyName.Should().Be("First Company Profile");
            firstProfile.Address.Should().Be("First address");
            firstProfile.GstNumber.Should().Be("24AAAAA1111A1Z5");

            secondProfile.Should().NotBeNull();
            secondProfile!.CompanyName.Should().Be("Second Company Profile");
            secondProfile.Address.Should().Be("Second address");
            secondProfile.GstNumber.Should().Be("24BBBBB2222B1Z5");
        }

        [Fact]
        public async Task Put_Should_Update_Only_Active_Company_Profile()
        {
            await AuthenticateAsync();
            var firstCompanyId = ActiveCompanyId;
            var secondCompanyId = await CreateOwnedCompanyAsync("Second Company");

            await Client.PutAsJsonAsync(
                "/api/company-profile",
                new { CompanyName = "First Profile" });

            SetActiveCompanyId(secondCompanyId);
            await Client.PutAsJsonAsync(
                "/api/company-profile",
                new { CompanyName = "Second Profile" });
            await Client.PutAsJsonAsync(
                "/api/company-profile",
                new { CompanyName = "Updated Second Profile" });

            SetActiveCompanyId(firstCompanyId);
            var firstProfile = await Client
                .GetFromJsonAsync<CompanyProfileResponse>("/api/company-profile");

            SetActiveCompanyId(secondCompanyId);
            var secondProfile = await Client
                .GetFromJsonAsync<CompanyProfileResponse>("/api/company-profile");

            firstProfile.Should().NotBeNull();
            firstProfile!.CompanyName.Should().Be("First Profile");
            secondProfile.Should().NotBeNull();
            secondProfile!.CompanyName.Should().Be("Updated Second Profile");
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
            Client.DefaultRequestHeaders.Remove("X-Company-Id");
            Client.DefaultRequestHeaders.Add(
                "X-Company-Id",
                user.CompanyId.ToString());
        }

        private async Task<TestUser> CreateUserAsync(string role)
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var unique = Guid.NewGuid().ToString("N");
            var user = new ApplicationUser
            {
                UserName = $"company_profile_{unique}",
                Email = $"company_profile_{unique}@example.com"
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

            return new TestUser(user.UserName!, password, company.Id);
        }

        private async Task<int> CreateOwnedCompanyAsync(string name)
        {
            var response = await Client.PostAsJsonAsync(
                "/api/companies",
                new { Name = name });
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var company = await response.Content.ReadFromJsonAsync<UserCompanyDto>();
            company.Should().NotBeNull();

            return company!.Id;
        }

        private async Task<int> CreateCompanyWithoutMembershipAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var company = new Company
            {
                Name = $"Unassigned company {Guid.NewGuid():N}",
                CreatedAtUtc = DateTime.UtcNow
            };

            context.Companies.Add(company);
            await context.SaveChangesAsync();

            return company.Id;
        }

        private sealed record TestUser(
            string UserName,
            string Password,
            int CompanyId);
    }
}
