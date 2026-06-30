using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Features.Auth.Register;
using InventoryManagement.Tests.IntegrationTests.Common;

namespace InventoryManagement.Tests.IntegrationTests.Auth
{
    public class RegisterTests : TestBase
    {
        public RegisterTests(CustomWebApplicationFactory factory) : base (factory)
        {
            
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
