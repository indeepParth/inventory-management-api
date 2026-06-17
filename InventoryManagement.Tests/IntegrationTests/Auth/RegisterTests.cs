using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Features.Auth.Register;
using InventoryManagement.Tests.IntegrationTests.Common;

namespace InventoryManagement.Tests.IntegrationTests.Auth
{
    public class RegisterTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public RegisterTests(
            CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Register_Should_Return_Ok()
        {
            // Arrange

            var request = new Command
            {
                UserName = "integrationuser",
                Email = "integration@test.com",
                Passward = "Password1"
            };

            // Act

            var response =
                await _client.PostAsJsonAsync(
                    "/api/auth/register",
                    request);

            // Assert

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result =
                await response.Content
                    .ReadFromJsonAsync<Responce>();

            result.Should().NotBeNull();
            result!.UserName.Should().Be("integrationuser");
        }
    }
}