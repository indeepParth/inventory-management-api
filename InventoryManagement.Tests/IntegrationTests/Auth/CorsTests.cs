using System.Net;
using FluentAssertions;
using InventoryManagement.Tests.IntegrationTests.Common;

namespace InventoryManagement.Tests.IntegrationTests.Auth
{
    public class CorsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public CorsTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Preflight_Should_Allow_Configured_Origin_Method_And_Headers()
        {
            using var request = CreatePreflightRequest(
                "http://localhost",
                HttpMethod.Get.Method,
                "Authorization, Content-Type");

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            response.Headers.GetValues("Access-Control-Allow-Origin")
                .Should()
                .Contain("http://localhost");
            response.Headers.GetValues("Access-Control-Allow-Methods")
                .Should()
                .Contain(header => header.Contains(HttpMethod.Get.Method));
            response.Headers.GetValues("Access-Control-Allow-Headers")
                .Should()
                .Contain(header =>
                    header.Contains("Authorization", StringComparison.OrdinalIgnoreCase) &&
                    header.Contains("Content-Type", StringComparison.OrdinalIgnoreCase));
            response.Headers.Contains("Access-Control-Allow-Credentials")
                .Should()
                .BeFalse();
        }

        [Fact]
        public async Task Preflight_Should_Not_Allow_Unconfigured_Origin()
        {
            using var request = CreatePreflightRequest(
                "https://evil.example.com",
                HttpMethod.Get.Method,
                "Authorization, Content-Type");

            var response = await _client.SendAsync(request);

            response.Headers.Contains("Access-Control-Allow-Origin")
                .Should()
                .BeFalse();
        }

        [Fact]
        public async Task Preflight_Should_Not_Allow_Unconfigured_Header()
        {
            using var request = CreatePreflightRequest(
                "http://localhost",
                HttpMethod.Get.Method,
                "X-Secret");

            var response = await _client.SendAsync(request);

            response.Headers.TryGetValues(
                    "Access-Control-Allow-Headers",
                    out var allowedHeaders)
                .Should()
                .BeTrue();

            allowedHeaders!
                .Should()
                .NotContain(header =>
                    header.Contains("X-Secret", StringComparison.OrdinalIgnoreCase));
        }

        private static HttpRequestMessage CreatePreflightRequest(
            string origin,
            string method,
            string headers)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Options,
                "/api/products");

            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", method);
            request.Headers.Add("Access-Control-Request-Headers", headers);

            return request;
        }
    }
}
