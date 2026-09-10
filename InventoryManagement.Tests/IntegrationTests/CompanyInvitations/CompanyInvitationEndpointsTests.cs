using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Authorization;
using InventoryManagement.Application.DTOs.User;
using InventoryManagement.Application.Features.Auth.Login;
using InventoryManagement.Application.Features.Auth.Register;
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

namespace InventoryManagement.Tests.IntegrationTests.CompanyInvitations
{
    public class CompanyInvitationEndpointsTests : TestBase
    {
        private readonly CustomWebApplicationFactory _factory;

        public CompanyInvitationEndpointsTests(
            CustomWebApplicationFactory factory) : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Owner_Should_Create_Invitation_With_Hashed_Token()
        {
            var owner = await RegisterAndAuthenticateAsync();

            var response = await Client.PostAsJsonAsync(
                "/api/company-invitations",
                new
                {
                    Email = $"invite_{Guid.NewGuid():N}@example.com",
                    Role = CompanyRoles.Staff
                });

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content
                .ReadFromJsonAsync<CreateInvitationResponse>();
            body.Should().NotBeNull();
            body!.Role.Should().Be(CompanyRoles.Staff);
            body.CompanyId.Should().Be(owner.CompanyId);
            body.AcceptUrl.Should().StartWith("/accept-invitation/");

            var token = body.AcceptUrl.Split('/').Last();

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var invitation = await context.CompanyInvitations
                .SingleAsync(x => x.Id == body.Id);

            invitation.TokenHash.Should().NotBe(token);
            invitation.TokenHash.Should().HaveLength(64);
            invitation.Status.Should().Be(CompanyInvitationStatuses.Pending);
        }

        [Fact]
        public async Task Admin_Should_Not_Invite_Admin_Or_Owner()
        {
            var owner = await RegisterAndAuthenticateAsync();
            var admin = await CreateUserAsync(CompanyRoles.Admin, owner.CompanyId);
            await AuthenticateAsAsync(admin.UserName, admin.Password, owner.CompanyId);

            var response = await Client.PostAsJsonAsync(
                "/api/company-invitations",
                new
                {
                    Email = $"invite_{Guid.NewGuid():N}@example.com",
                    Role = CompanyRoles.Admin
                });

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Manager_Should_Not_Create_Invitation()
        {
            var owner = await RegisterAndAuthenticateAsync();
            var manager = await CreateUserAsync(CompanyRoles.Manager, owner.CompanyId);
            await AuthenticateAsAsync(manager.UserName, manager.Password, owner.CompanyId);

            var response = await Client.PostAsJsonAsync(
                "/api/company-invitations",
                new
                {
                    Email = $"invite_{Guid.NewGuid():N}@example.com",
                    Role = CompanyRoles.Staff
                });

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Duplicate_Pending_Invitation_Should_Be_Rejected_Case_Insensitive()
        {
            await RegisterAndAuthenticateAsync();
            var unique = Guid.NewGuid().ToString("N");

            var first = await Client.PostAsJsonAsync(
                "/api/company-invitations",
                new
                {
                    Email = $"Invite_{unique}@example.com",
                    Role = CompanyRoles.Staff
                });
            first.EnsureSuccessStatusCode();

            var duplicate = await Client.PostAsJsonAsync(
                "/api/company-invitations",
                new
                {
                    Email = $"invite_{unique}@example.com",
                    Role = CompanyRoles.Viewer
                });

            duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Existing_User_Should_Accept_Only_Matching_Email_Invitation()
        {
            var owner = await RegisterAndAuthenticateAsync();
            var invitedUser = await CreateUserAsync();
            var invite = await CreateInviteAsync(invitedUser.Email, CompanyRoles.Viewer);
            await AuthenticateAsAsync(invitedUser.UserName, invitedUser.Password);

            var response = await Client.PostAsJsonAsync(
                $"/api/company-invitations/{invite.Token}/accept",
                new { Mode = "existingUser" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var membership = await context.CompanyUsers
                .SingleOrDefaultAsync(x =>
                    x.CompanyId == owner.CompanyId &&
                    x.UserId == invitedUser.Id);
            membership.Should().NotBeNull();
            membership!.Role.Should().Be(CompanyRoles.Viewer);
        }

        [Fact]
        public async Task Existing_User_Should_Not_Accept_Mismatched_Email_Invitation()
        {
            await RegisterAndAuthenticateAsync();
            var otherUser = await CreateUserAsync();
            var invite = await CreateInviteAsync(
                $"different_{Guid.NewGuid():N}@example.com",
                CompanyRoles.Staff);
            await AuthenticateAsAsync(otherUser.UserName, otherUser.Password);

            var response = await Client.PostAsJsonAsync(
                $"/api/company-invitations/{invite.Token}/accept",
                new { Mode = "existingUser" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task New_User_Acceptance_Should_Create_User_Without_Global_Roles()
        {
            var owner = await RegisterAndAuthenticateAsync();
            var email = $"new_invited_{Guid.NewGuid():N}@example.com";
            var invite = await CreateInviteAsync(email, CompanyRoles.Staff);
            Client.DefaultRequestHeaders.Authorization = null;
            Client.DefaultRequestHeaders.Remove("X-Company-Id");

            var response = await Client.PostAsJsonAsync(
                $"/api/company-invitations/{invite.Token}/accept",
                new
                {
                    Mode = "newUser",
                    UserName = $"invited_{Guid.NewGuid():N}",
                    Password = "Password123",
                    ConfirmPassword = "Password123"
                });

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var user = await userManager.FindByEmailAsync(email);
            user.Should().NotBeNull();
            var roles = await userManager.GetRolesAsync(user!);
            roles.Should().BeEmpty();

            var membership = await context.CompanyUsers.SingleOrDefaultAsync(x =>
                x.CompanyId == owner.CompanyId &&
                x.UserId == user!.Id);
            membership.Should().NotBeNull();
            membership!.Role.Should().Be(CompanyRoles.Staff);

            var subscription = await context.UserSubscriptions
                .Include(x => x.Plan)
                .SingleOrDefaultAsync(x => x.UserId == user!.Id);
            subscription.Should().NotBeNull();
            subscription!.Status.Should().Be(SubscriptionStatuses.Active);
            subscription.Plan.Code.Should().Be(SubscriptionPlans.Free);
        }

        [Fact]
        public async Task Revoked_Invitation_Should_Not_Be_Accepted()
        {
            await RegisterAndAuthenticateAsync();
            var invite = await CreateInviteAsync(
                $"revoked_{Guid.NewGuid():N}@example.com",
                CompanyRoles.Staff);

            var revoke = await Client.DeleteAsync(
                $"/api/company-invitations/{invite.Id}");
            revoke.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var inspect = await Client.GetAsync(
                $"/api/company-invitations/{invite.Token}");
            inspect.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        private async Task<RegisteredUser> RegisterAndAuthenticateAsync()
        {
            var unique = Guid.NewGuid().ToString("N");
            var userName = $"owner_{unique}";
            const string password = "Password123";

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

            await AuthenticateAsAsync(userName, password);

            var companyName = $"{userName}'s Company";
            var companyResponse = await Client.PostAsJsonAsync(
                "/api/companies",
                new { Name = companyName });
            companyResponse.EnsureSuccessStatusCode();

            var company = await companyResponse.Content.ReadFromJsonAsync<UserCompanyDto>();
            company.Should().NotBeNull();

            Client.DefaultRequestHeaders.Remove("X-Company-Id");
            Client.DefaultRequestHeaders.Add("X-Company-Id", company!.Id.ToString());

            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByNameAsync(userName);
            user.Should().NotBeNull();

            return new RegisteredUser(
                user!.Id,
                userName,
                $"{userName}@example.com",
                password,
                company.Id);
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

            var createResult = await userManager.CreateAsync(user, password);
            createResult.Succeeded.Should().BeTrue();

            if (role is not null)
            {
                context.CompanyUsers.Add(new CompanyUser
                {
                    CompanyId = companyId!.Value,
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
                password);
        }

        private async Task<CreatedInvite> CreateInviteAsync(
            string email,
            string role)
        {
            var response = await Client.PostAsJsonAsync(
                "/api/company-invitations",
                new
                {
                    Email = email,
                    Role = role
                });
            response.EnsureSuccessStatusCode();

            var invite = await response.Content
                .ReadFromJsonAsync<CreateInvitationResponse>();
            invite.Should().NotBeNull();

            return new CreatedInvite(
                invite!.Id,
                invite.AcceptUrl.Split('/').Last());
        }

        private async Task AuthenticateAsAsync(
            string userName,
            string password,
            int? companyId = null)
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

            if (companyId is not null)
            {
                Client.DefaultRequestHeaders.Add(
                    "X-Company-Id",
                    companyId.Value.ToString());
            }
        }

        private sealed record RegisteredUser(
            string Id,
            string UserName,
            string Email,
            string Password,
            int CompanyId);

        private sealed record TestUser(
            string Id,
            string UserName,
            string Email,
            string Password);

        private sealed record CreatedInvite(int Id, string Token);

        private sealed class CreateInvitationResponse
        {
            public int Id { get; set; }
            public int CompanyId { get; set; }
            public string CompanyName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string AcceptUrl { get; set; } = string.Empty;
        }
    }
}
