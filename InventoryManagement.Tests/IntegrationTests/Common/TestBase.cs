using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using InventoryManagement.Infrastructure.Identity;
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
            Client.DefaultRequestHeaders.Add(
                "X-Company-Id",
                register!.CompanyId.ToString());
            ActiveCompanyId = register.CompanyId;
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
    }
}
