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
            var request = new Command
            {
                UserName = "test_User",
                Email = "test@user.com",
                Passward = "123456789"
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
                    .ReadFromJsonAsync<Responce>();

            result.Should().NotBeNull();
            result.UserName.Should().Be("test_User");
            result.Email.Should().Be("test@user.com");
        }

        [Fact]
        public async Task Register_Should_Reject_Duplicate_Email()
        {
            // Arrange
            var request = new Command
            {
                UserName = "test_User",
                Email = "test@user.com",
                Passward = "123456789"
            };

            // First Register
            await Client.PostAsJsonAsync("/api/auth/register", request);

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

            var body = await response.Content.ReadAsStringAsync();

            body.Should().Contain("already");
        }

        [Fact]
        public async Task Register_Should_Reject_Weak_Password()
        {
            var request = new Command
            {
                UserName = "test_User",
                Email = "test@user.com",
                Passward = "123"
            };

            var response = await Client.PostAsJsonAsync("/api/auth/register", request);

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task Register_Should_Reject_Empty_UserName()
        {
            var request = new Command
            {
                UserName = "",
                Email = "test@user.com",
                Passward = "123456789"
            };

            var response = await Client.PostAsJsonAsync("/api/auth/register", request);

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }
    }
}