using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Authorization;
using InventoryManagement.Application.Features.Auth.Login;
using InventoryManagement.Infrastructure.Identity;
using InventoryManagement.Tests.IntegrationTests.Common;
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
            var admin = await CreateUserAsync(ApplicationRoles.Admin);
            var salesUser = await CreateUserAsync(ApplicationRoles.Sales);
            await AuthenticateAsAsync(Client, admin.UserName, admin.Password);

            var response = await Client.GetAsync("/api/users");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain(salesUser.UserName);
            body.Should().Contain(ApplicationRoles.Sales);
            body.Should().NotContain("passwordHash");
            body.Should().NotContain("securityStamp");
            body.Should().NotContain("concurrencyStamp");
            body.Should().NotContain("refreshToken");
        }

        [Fact]
        public async Task Me_Should_Return_Current_User_Details()
        {
            var user = await CreateUserAsync(ApplicationRoles.Sales);
            await AuthenticateAsAsync(Client, user.UserName, user.Password);

            var response = await Client.GetAsync("/api/users/me");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain(user.UserName);
            body.Should().Contain(user.Email);
            body.Should().Contain(ApplicationRoles.Sales);
            body.Should().Contain("isDisabled");
        }

        [Fact]
        public async Task ChangePassword_Should_Update_Current_User_Password()
        {
            var user = await CreateUserAsync(ApplicationRoles.Sales);
            await AuthenticateAsAsync(Client, user.UserName, user.Password);

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
            var user = await CreateUserAsync(ApplicationRoles.Sales);
            await AuthenticateAsAsync(Client, user.UserName, user.Password);

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
        public async Task CreateUser_Should_Create_User_With_Valid_Roles()
        {
            var admin = await CreateUserAsync(ApplicationRoles.Admin);
            await AuthenticateAsAsync(Client, admin.UserName, admin.Password);
            var unique = Guid.NewGuid().ToString("N");
            var userName = $"created_{unique}";

            var response = await Client.PostAsJsonAsync(
                "/api/users",
                new
                {
                    UserName = userName,
                    Email = $"{userName}@example.com",
                    Password = "Password123",
                    Roles = new[] { ApplicationRoles.Sales }
                });

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain(userName);
            body.Should().Contain(ApplicationRoles.Sales);

            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();
            var createdUser = await userManager.FindByNameAsync(userName);
            createdUser.Should().NotBeNull();
            createdUser!.Email.Should().Be($"{userName}@example.com");
            (await userManager.IsInRoleAsync(
                createdUser,
                ApplicationRoles.Sales)).Should().BeTrue();
        }

        [Fact]
        public async Task CreateUser_Should_Reject_Non_Admin_User()
        {
            var user = await CreateUserAsync(ApplicationRoles.Sales);
            await AuthenticateAsAsync(Client, user.UserName, user.Password);

            var response = await Client.PostAsJsonAsync(
                "/api/users",
                new
                {
                    UserName = $"created_{Guid.NewGuid():N}",
                    Email = "created@example.com",
                    Password = "Password123",
                    Roles = new[] { ApplicationRoles.Sales }
                });

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task AssignRole_Should_Add_Supported_Role()
        {
            var admin = await CreateUserAsync(ApplicationRoles.Admin);
            var user = await CreateUserAsync();
            await AuthenticateAsAsync(Client, admin.UserName, admin.Password);

            var response = await Client.PostAsJsonAsync(
                $"/api/users/{user.Id}/roles",
                new { Role = ApplicationRoles.Inventory });

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            await UserShouldBeInRoleAsync(user.Id, ApplicationRoles.Inventory);
        }

        [Fact]
        public async Task AssignRole_Should_Reject_Unsupported_Role()
        {
            var admin = await CreateUserAsync(ApplicationRoles.Admin);
            var user = await CreateUserAsync();
            await AuthenticateAsAsync(Client, admin.UserName, admin.Password);

            var response = await Client.PostAsJsonAsync(
                $"/api/users/{user.Id}/roles",
                new { Role = "Owner" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task RemoveRole_Should_Remove_Role()
        {
            var admin = await CreateUserAsync(ApplicationRoles.Admin);
            var user = await CreateUserAsync(ApplicationRoles.Manager);
            await AuthenticateAsAsync(Client, admin.UserName, admin.Password);

            var response = await Client.DeleteAsync(
                $"/api/users/{user.Id}/roles/{ApplicationRoles.Manager}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            await UserShouldNotBeInRoleAsync(user.Id, ApplicationRoles.Manager);
        }

        [Fact]
        public async Task RemoveRole_Should_Prevent_Admin_Removing_Own_Final_Admin_Role()
        {
            var admin = await CreateUserAsync(ApplicationRoles.Admin);
            await MakeOnlyActiveAdminAsync(admin.Id);
            await AuthenticateAsAsync(Client, admin.UserName, admin.Password);

            var response = await Client.DeleteAsync(
                $"/api/users/{admin.Id}/roles/{ApplicationRoles.Admin}");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("final Admin role");
        }

        [Fact]
        public async Task DisableUser_Should_Prevent_Disabling_Final_Active_Admin()
        {
            var admin = await CreateUserAsync(ApplicationRoles.Admin);
            await MakeOnlyActiveAdminAsync(admin.Id);
            await AuthenticateAsAsync(Client, admin.UserName, admin.Password);

            var response = await Client.PostAsync(
                $"/api/users/{admin.Id}/disable",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("final active administrator");
        }

        [Fact]
        public async Task DisableAndEnableUser_Should_Update_Disabled_State()
        {
            var admin = await CreateUserAsync(ApplicationRoles.Admin);
            var user = await CreateUserAsync();
            await AuthenticateAsAsync(Client, admin.UserName, admin.Password);

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
            params string[] roles)
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

            foreach (var role in ApplicationRoles.All)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

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

            foreach (var role in roles)
            {
                var addRoleResult = await userManager.AddToRoleAsync(
                    user,
                    role);
                addRoleResult.Succeeded.Should().BeTrue();
            }

            return new TestUser(user.Id, user.UserName!, user.Email!, password);
        }

        private static async Task AuthenticateAsAsync(
            HttpClient client,
            string userName,
            string password)
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
        }

        private async Task UserShouldBeInRoleAsync(
            string userId,
            string role)
        {
            var isInRole = await IsUserInRoleAsync(userId, role);

            isInRole.Should().BeTrue();
        }

        private async Task UserShouldNotBeInRoleAsync(
            string userId,
            string role)
        {
            var isInRole = await IsUserInRoleAsync(userId, role);

            isInRole.Should().BeFalse();
        }

        private async Task<bool> IsUserInRoleAsync(
            string userId,
            string role)
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(userId);
            user.Should().NotBeNull();

            return await userManager.IsInRoleAsync(user!, role);
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

        private async Task MakeOnlyActiveAdminAsync(string adminUserId)
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            var admins = await userManager.GetUsersInRoleAsync(
                ApplicationRoles.Admin);

            foreach (var admin in admins.Where(x => x.Id != adminUserId))
            {
                var result = await userManager.RemoveFromRoleAsync(
                    admin,
                    ApplicationRoles.Admin);

                result.Succeeded.Should().BeTrue();
            }
        }

        private sealed record TestUser(
            string Id,
            string UserName,
            string Email,
            string Password);
    }
}
