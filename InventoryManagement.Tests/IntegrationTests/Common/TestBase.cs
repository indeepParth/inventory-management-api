using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using InventoryManagement.Application.Authorization;
using InventoryManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using LoginCommand = InventoryManagement.Application.Features.Auth.Login.Command;
using LoginResponse = InventoryManagement.Application.Features.Auth.Login.Response;
using RegisterCommand = InventoryManagement.Application.Features.Auth.Register.Command;

namespace InventoryManagement.Tests.IntegrationTests.Common
{
    public abstract class TestBase : IClassFixture<CustomWebApplicationFactory>
    {
        protected readonly HttpClient Client;
        private readonly CustomWebApplicationFactory _factory;

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

            await Client.PostAsJsonAsync("/api/auth/register", new RegisterCommand
            {
                UserName = username,
                Email = $"{username}@user.com",
                Password = password
            });

            await AssignRoleAsync(username, ApplicationRoles.Admin);

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
        }

        private async Task AssignRoleAsync(
            string username,
            string role)
        {
            using var scope = _factory.Services.CreateScope();
            var roleManager = scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            if (!await roleManager.RoleExistsAsync(role))
            {
                var roleResult = await roleManager.CreateAsync(
                    new IdentityRole(role));
                roleResult.Succeeded.Should().BeTrue();
            }

            var user = await userManager.FindByNameAsync(username);
            user.Should().NotBeNull();

            var addResult = await userManager.AddToRoleAsync(user!, role);
            addResult.Succeeded.Should().BeTrue();
        }
    }
}
