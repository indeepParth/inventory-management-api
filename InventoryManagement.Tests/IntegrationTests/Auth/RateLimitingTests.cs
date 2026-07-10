using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Features.Auth.Login;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace InventoryManagement.Tests.IntegrationTests.Auth
{
    public class RateLimitingTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public RateLimitingTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GlobalLimiter_Should_Return_Structured_TooManyRequests()
        {
            using var client = CreateClient(new Dictionary<string, string?>
            {
                ["RateLimiting:Global:PermitLimit"] = "1",
                ["RateLimiting:Global:WindowSeconds"] = "60",
                ["RateLimiting:Login:PermitLimit"] = "100",
                ["RateLimiting:Register:PermitLimit"] = "100",
                ["RateLimiting:RefreshToken:PermitLimit"] = "100"
            });

            await client.GetAsync("/api/products");

            var response = await client.GetAsync("/api/products");

            await AssertTooManyRequestsAsync(response);
        }

        [Fact]
        public async Task LoginLimiter_Should_Return_Structured_TooManyRequests()
        {
            using var client = CreateClient(AuthLimiterSettings("Login"));
            var request = new Command
            {
                UserName = $"missing_{Guid.NewGuid():N}",
                Password = "wrong-password"
            };

            await client.PostAsJsonAsync("/api/auth/login", request);

            var response = await client.PostAsJsonAsync("/api/auth/login", request);

            await AssertTooManyRequestsAsync(response);
        }

        [Fact]
        public async Task RegisterLimiter_Should_Return_Structured_TooManyRequests()
        {
            using var client = CreateClient(AuthLimiterSettings("Register"));
            var request = new Application.Features.Auth.Register.Command
            {
                UserName = $"limited_{Guid.NewGuid():N}",
                Email = $"limited_{Guid.NewGuid():N}@user.com",
                Password = "123456789"
            };

            await client.PostAsJsonAsync("/api/auth/register", request);

            var response = await client.PostAsJsonAsync("/api/auth/register", request);

            await AssertTooManyRequestsAsync(response);
        }

        [Fact]
        public async Task RefreshTokenLimiter_Should_Return_Structured_TooManyRequests()
        {
            using var client = CreateClient(AuthLimiterSettings("RefreshToken"));
            var request = new Application.Features.Auth.RefreshAccessToken.Command
            {
                RefreshToken = $"missing_{Guid.NewGuid():N}"
            };

            await client.PostAsJsonAsync("/api/auth/refreshtoken", request);

            var response = await client.PostAsJsonAsync(
                "/api/auth/refreshtoken",
                request);

            await AssertTooManyRequestsAsync(response);
        }

        private HttpClient CreateClient(Dictionary<string, string?> settings)
        {
            return _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, configuration) =>
                {
                    configuration.AddInMemoryCollection(settings);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
        }

        private static Dictionary<string, string?> AuthLimiterSettings(
            string limitedPolicyName)
        {
            var settings = new Dictionary<string, string?>
            {
                ["RateLimiting:Global:PermitLimit"] = "100",
                ["RateLimiting:Global:WindowSeconds"] = "60",
                ["RateLimiting:Login:PermitLimit"] = "100",
                ["RateLimiting:Register:PermitLimit"] = "100",
                ["RateLimiting:RefreshToken:PermitLimit"] = "100"
            };

            settings[$"RateLimiting:{limitedPolicyName}:PermitLimit"] = "1";
            settings[$"RateLimiting:{limitedPolicyName}:WindowSeconds"] = "60";

            return settings;
        }

        private static async Task AssertTooManyRequestsAsync(
            HttpResponseMessage response)
        {
            response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter.Should().NotBeNull();

            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("\"statusCode\":429");
            body.Should().Contain("\"message\":\"Too many requests.\"");
            body.Should().Contain("\"traceId\":");
        }
    }
}
