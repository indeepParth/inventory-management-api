using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Features.Auth.Login;
using InventoryManagement.Tests.IntegrationTests.Common;

namespace InventoryManagement.Tests.IntegrationTests.Auth
{
    public class LoginTests : TestBase
    {
        public LoginTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Login_Should_Return_Unauthorized_For_Bad_Credentials()
        {
            var request = new Command
            {
                UserName = $"missing_{Guid.NewGuid():N}",
                Password = "wrong-password"
            };

            var response = await Client.PostAsJsonAsync("/api/auth/login", request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("statusCode");
            body.Should().Contain("traceId");
        }
    }
}
