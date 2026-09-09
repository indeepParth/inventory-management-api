using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Authorization;
using InventoryManagement.Application.Features.Auth.Login;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Identity;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryManagement.Tests.IntegrationTests.Auth
{
    public class UserManagementEndpointsTests : TestBase
    {
        private readonly CustomWebApplicationFactory _factory;

        public UserManagementEndpointsTests(
            CustomWebApplicationFactory factory) : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetUsers_Should_Require_Authentication()
        {
            var response = await Client.GetAsync("/api/users");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetUsers_Should_Reject_Non_Admin_User()
        {
            var user = await CreateUserAsync();
            await AuthenticateAsAsync(Client, user.UserName, user.Password);

            var response = await Client.GetAsync("/api/users");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetUsers_Should_List_Users_With_Roles_Without_Secrets()
        {
            var admin = await CreateUserAsync(CompanyRoles.Owner);
            var staffUser = await CreateUserAsync(CompanyRoles.Staff, admin.CompanyId);
            await AuthenticateAsAsync(Client, admin.UserName, admin.Password, admin.CompanyId);

            var response = await Client.GetAsync("/api/users");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain(staffUser.UserName);
            body.Should().Contain(CompanyRoles.Staff);
            body.Should().NotContain("passwordHash");
            body.Should().NotContain("securityStamp");
            body.Should().NotContain("concurrencyStamp");
            body.Should().NotContain("refreshToken");
        }

        [Fact]
        public async Task Me_Should_Return_Current_User_Details()
        {
            var user = await CreateUserAsync(CompanyRoles.Staff);
            await AuthenticateAsAsync(Client, user.UserName, user.Password, user.CompanyId);

            var response = await Client.GetAsync("/api/users/me");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain(user.UserName);
            body.Should().Contain(user.Email);
            body.Should().Contain(CompanyRoles.Staff);
            body.Should().Contain("isDisabled");
        }

        [Fact]
        public async Task ChangePassword_Should_Update_Current_User_Password()
        {
            var user = await CreateUserAsync(CompanyRoles.Staff);
            await AuthenticateAsAsync(Client, user.UserName, user.Password, user.CompanyId);

            var response = await Client.PostAsJsonAsync(
                "/api/users/me/change-password",
                new
                {
                    CurrentPassword = user.Password,
                    NewPassword = "NewPassword123",
                    ConfirmPassword = "NewPassword123"
                });

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            await AuthenticateAsAsync(Client, user.UserName, "NewPassword123");
        }

        [Fact]
        public async Task ChangePassword_Should_Reject_Wrong_Current_Password()
        {
            var user = await CreateUserAsync(CompanyRoles.Staff);
            await AuthenticateAsAsync(Client, user.UserName, user.Password, user.CompanyId);

            var response = await Client.PostAsJsonAsync(
                "/api/users/me/change-password",
                new
                {
                    CurrentPassword = "WrongPassword123",
                    NewPassword = "NewPassword123",
                    ConfirmPassword = "NewPassword123"
                });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateUser_Should_Return_BadRequest_Because_Invitations_Are_Required()
        {
            var admin = await CreateUserAsync(CompanyRoles.Owner);
            await AuthenticateAsAsync(Client, admin.UserName, admin.Password, admin.CompanyId);
            var unique = Guid.NewGuid().ToString("N");
            var userName = $"created_{unique}";

            var response = await Client.PostAsJsonAsync(
                "/api/users",
                new
                {
                    UserName = userName,
                    Email = $"{userName}@example.com",
                    Password = "Password123",
                    Roles = new[] { CompanyRoles.Staff }
                });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("company invitations");

            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();
            var createdUser = await userManager.FindByNameAsync(userName);
            createdUser.Should().BeNull();
        }

        [Fact]
        public async Task CreateUser_Should_Reject_Non_Admin_User()
        {
            var user = await CreateUserAsync(CompanyRoles.Staff);
            await AuthenticateAsAsync(Client, user.UserName, user.Password, user.CompanyId);

            var response = await Client.PostAsJsonAsync(
                "/api/users",
                new
                {
                    UserName = $"created_{Guid.NewGuid():N}",
                    Email = "created@example.com",
                    Password = "Password123",
                    Roles = new[] { CompanyRoles.Staff }
                });

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task AssignRole_Should_Update_Existing_Company_Member_Role()
        {
            var admin = await CreateUserAsync(CompanyRoles.Owner);
            var user = await CreateUserAsync(CompanyRoles.Viewer, admin.CompanyId);
            await AuthenticateAsAsync(Client, admin.UserName, admin.Password, admin.CompanyId);

            var response = await Client.PostAsJsonAsync(
                $"/api/users/{user.Id}/roles",
                new { Role = CompanyRoles.Staff });

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            await UserShouldBeInRoleAsync(
                user.Id,
                CompanyRoles.Staff,
                admin.CompanyId!.Value);
        }

        [Fact]
        public async Task AssignRole_Should_Return_NotFound_When_User_Is_Not_Company_Member()
        {
            var admin = await CreateUserAsync(CompanyRoles.Owner);
            var user = await CreateUserAsync();
            await AuthenticateAsAsync(Client, admin.UserName, admin.Password, admin.CompanyId);

            var response = await Client.PostAsJsonAsync(
                $"/api/users/{user.Id}/roles",
                new { Role = CompanyRoles.Viewer });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task AssignRole_Should_Prevent_Admin_Managing_Privileged_Roles()
        {
            var owner = await CreateUserAsync(CompanyRoles.Owner);
            var admin = await CreateUserAsync(CompanyRoles.Admin, owner.CompanyId);
            var user = await CreateUserAsync(CompanyRoles.Manager, owner.CompanyId);
            await AuthenticateAsAsync(Client, admin.UserName, admin.Password, owner.CompanyId);

            var response = await Client.PostAsJsonAsync(
                $"/api/users/{user.Id}/roles",
                new { Role = CompanyRoles.Admin });

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task AssignRole_Should_Allow_Owner_Managing_Admin_Role()
        {
            var owner = await CreateUserAsync(CompanyRoles.Owner);
            var user = await CreateUserAsync(CompanyRoles.Manager, owner.CompanyId);
            await AuthenticateAsAsync(Client, owner.UserName, owner.Password, owner.CompanyId);

            var response = await Client.PostAsJsonAsync(
                $"/api/users/{user.Id}/roles",
                new { Role = CompanyRoles.Admin });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            await UserShouldBeInRoleAsync(
                user.Id,
                CompanyRoles.Admin,
                owner.CompanyId!.Value);
        }

        [Fact]
        public async Task AssignRole_Should_Reject_Unsupported_Role()
        {
            var admin = await CreateUserAsync(CompanyRoles.Owner);
            var user = await CreateUserAsync();
            await AuthenticateAsAsync(Client, admin.UserName, admin.Password, admin.CompanyId);

            var response = await Client.PostAsJsonAsync(
                $"/api/users/{user.Id}/roles",
                new { Role = "Sales" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task RemoveRole_Should_Remove_Role()
        {
            var admin = await CreateUserAsync(CompanyRoles.Owner);
            var user = await CreateUserAsync(CompanyRoles.Manager, admin.CompanyId);
            await AuthenticateAsAsync(Client, admin.UserName, admin.Password, admin.CompanyId);

            var response = await Client.DeleteAsync(
                $"/api/users/{user.Id}/roles/{CompanyRoles.Manager}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            await UserShouldNotBeInRoleAsync(
                user.Id,
                CompanyRoles.Manager,
                admin.CompanyId!.Value);
        }

        [Fact]
        public async Task RemoveRole_Should_Prevent_Owner_Removing_Own_Final_Owner_Role()
        {
            var admin = await CreateUserAsync(CompanyRoles.Owner);
            await AuthenticateAsAsync(Client, admin.UserName, admin.Password, admin.CompanyId);

            var response = await Client.DeleteAsync(
                $"/api/users/{admin.Id}/roles/{CompanyRoles.Owner}");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("final active Owner role");
        }

        [Fact]
        public async Task DisableUser_Should_Prevent_Disabling_Final_Active_Owner()
        {
            var admin = await CreateUserAsync(CompanyRoles.Owner);
            await AuthenticateAsAsync(Client, admin.UserName, admin.Password, admin.CompanyId);

            var response = await Client.PostAsync(
                $"/api/users/{admin.Id}/disable",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("final active owner");
        }

        [Fact]
        public async Task DisableAndEnableUser_Should_Update_Disabled_State()
        {
            var admin = await CreateUserAsync(CompanyRoles.Owner);
            var user = await CreateUserAsync();
            await AuthenticateAsAsync(Client, admin.UserName, admin.Password, admin.CompanyId);

            var disableResponse = await Client.PostAsync(
                $"/api/users/{user.Id}/disable",
                null);

            disableResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            await UserShouldBeDisabledAsync(user.Id);

            var enableResponse = await Client.PostAsync(
                $"/api/users/{user.Id}/enable",
                null);

            enableResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            await UserShouldBeEnabledAsync(user.Id);
        }

        private async Task<TestUser> CreateUserAsync(
            string? role = null,
            int? companyId = null)
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var unique = Guid.NewGuid().ToString("N");
            var user = new ApplicationUser
            {
                UserName = $"user_{unique}",
                Email = $"user_{unique}@example.com"
            };
            const string password = "Password123";

            var createResult = await userManager.CreateAsync(
                user,
                password);
            createResult.Succeeded.Should().BeTrue();

            var effectiveCompanyId = companyId;

            if (role is not null)
            {
                if (effectiveCompanyId is null)
                {
                    var company = new Company
                    {
                        Name = $"{user.UserName}'s Company",
                        BillingOwnerUserId = user.Id,
                        CreatedAtUtc = DateTime.UtcNow
                    };

                    context.Companies.Add(company);
                    await context.SaveChangesAsync();
                    effectiveCompanyId = company.Id;
                }

                context.CompanyUsers.Add(new CompanyUser
                {
                    CompanyId = effectiveCompanyId.Value,
                    UserId = user.Id,
                    Role = role,
                    CreatedAtUtc = DateTime.UtcNow
                });

                await context.SaveChangesAsync();
            }

            return new TestUser(
                user.Id,
                user.UserName!,
                user.Email!,
                password,
                effectiveCompanyId);
        }

        private static async Task AuthenticateAsAsync(
            HttpClient client,
            string userName,
            string password,
            int? companyId = null)
        {
            var response = await client.PostAsJsonAsync(
                "/api/auth/login",
                new Command
                {
                    UserName = userName,
                    Password = password
                });

            response.EnsureSuccessStatusCode();

            var login = await response.Content.ReadFromJsonAsync<Response>();
            login.Should().NotBeNull();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    login!.AccessToken);

            client.DefaultRequestHeaders.Remove("X-Company-Id");

            if (companyId is not null)
            {
                client.DefaultRequestHeaders.Add(
                    "X-Company-Id",
                    companyId.Value.ToString());
            }
        }

        private async Task UserShouldBeInRoleAsync(
            string userId,
            string role,
            int companyId)
        {
            var isInRole = await IsUserInRoleAsync(userId, role, companyId);

            isInRole.Should().BeTrue();
        }

        private async Task UserShouldNotBeInRoleAsync(
            string userId,
            string role,
            int companyId)
        {
            var isInRole = await IsUserInRoleAsync(userId, role, companyId);

            isInRole.Should().BeFalse();
        }

        private async Task<bool> IsUserInRoleAsync(
            string userId,
            string role,
            int companyId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            return await context.CompanyUsers
                .AnyAsync(x =>
                    x.UserId == userId &&
                    x.CompanyId == companyId &&
                    x.Role == role);
        }

        private async Task UserShouldBeDisabledAsync(string userId)
        {
            var user = await FindUserAsync(userId);

            user.LockoutEnd.Should().NotBeNull();
            user.LockoutEnd!.Value.Should().BeAfter(DateTimeOffset.UtcNow);
        }

        private async Task UserShouldBeEnabledAsync(string userId)
        {
            var user = await FindUserAsync(userId);

            user.LockoutEnd.Should().BeNull();
        }

        private async Task<ApplicationUser> FindUserAsync(string userId)
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(userId);
            user.Should().NotBeNull();

            return user!;
        }

        private sealed record TestUser(
            string Id,
            string UserName,
            string Email,
            string Password,
            int? CompanyId);
    }
}
